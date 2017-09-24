// these have to be present, since the System.Diagnostics.Debug 
// class is marked [Conditional("DEBUG")] and the Trace class is
// marked with [Conditional("TRACE")] 
#define TRACE
#define DEBUG

using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Itp.Win32.MdiHook.NativeMethods;

namespace Itp.Win32.MdiHook.Server
{
    internal sealed class HookManager
    {
        private readonly object syncRegistrations;
        private readonly int mainThreadId;
        private ImmutableDictionary<IntPtr, HookRegistration> Registrations = ImmutableDictionary<IntPtr, HookRegistration>.Empty;
        private HookLifetimeManager Hook;
        private HookThreadManager CallbackManager;

        private delegate void HookCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr lResult);

        public HookManager()
        {
            mainThreadId = GetCurrentThreadId();
            syncRegistrations = new object();
            // we basically leak the CallbackManager, but there should be only one per process
            // since there should be only one HookManager, via one HookedProcess
            CallbackManager = new HookThreadManager();
        }

        private void QueueCallback(Action action) => CallbackManager.QueueCallback(action);

        public IWindowHook AddRegistration(HookRegistrationRecord hook, IDcsMarshalledObserver observer)
        {
            Contract.Requires(observer != null);

            var newReg = new HookRegistration(hook, observer, this);
            lock (syncRegistrations)
            {
                Registrations = Registrations.SetItem(hook.HWnd, newReg);
                Debug.WriteLine("Added hook for {0:x8}", hook.HWnd.ToInt32());

                if (Hook == null)
                {
                    Debug.WriteLine("Starting ITP Hook for thread {0}", mainThreadId);
                    Hook = new HookLifetimeManager(Hook_HookCallback);
                }
            }
            return newReg;
        }

        private void RemoveRegistration(HookRegistration hook)
        {
            lock (syncRegistrations)
            {
                Registrations = Registrations.Remove(hook.HWnd);
                Debug.WriteLine("Removed hook for {0:x8}", hook.HWnd.ToInt32());

                if (Registrations.IsEmpty && Hook != null)
                {
                    Debug.WriteLine("Shutting down ITP Hook for thread {0}", mainThreadId);
                    Hook.Dispose();
                    Hook = null;
                }
            }
        }

        private void Hook_HookCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr lResult)
        {
            HookRegistration hr;
            if (!Registrations.TryGetValue(hWnd, out hr))
            {
                return;
            }

            hr.HookCallback(hWnd, msg, wParam, lParam, lResult);
        }

        private sealed class HookThreadManager : IDisposable
        {
            private readonly BlockingCollection<Action> Messages;
            private readonly Thread thDeliver;

            public HookThreadManager()
            {
                this.Messages = new BlockingCollection<Action>();

                this.thDeliver = new Thread(thDeliver_Main);
                this.thDeliver.Name = "ITP Message Hook";
                this.thDeliver.IsBackground = true;
                this.thDeliver.Start();
            }

            public void Dispose()
            {
                this.Messages.CompleteAdding();
                this.thDeliver.Join();
            }

            private void thDeliver_Main()
            {
                try
                {
                    foreach (var callback in Messages.GetConsumingEnumerable())
                    {
                        callback();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in thDeliver_Main: " + ex.ToString());
                }
            }

            public void QueueCallback(Action action)
            {
                Messages.Add(action);
            }
        }

        private sealed class HookRegistration : IWindowHook
        {
            public readonly IntPtr HWnd;
            private readonly IMessageHookHandler<object> Handler;
            private readonly DcsMarshalledProxy Target;

            private readonly HookManager Parent;

            public HookRegistration(HookRegistrationRecord hook, IDcsMarshalledObserver observer, HookManager parent)
            {
                Contract.Requires(observer != null);

                this.HWnd = hook.HWnd;
                this.Parent = parent;
                this.Handler = hook.CreateHandler();
                this.Target = new DcsMarshalledProxy(observer, hook.ResultType);
            }

            public void HookCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr lResult)
            {
                object result;
                try
                {
                    result = Handler.HandleMessage(hWnd, msg, wParam, lParam, lResult);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in Handler.HandleMessage {0:x8}:\r\n{1}", HWnd.ToInt32(), ex);
                    Callback(() => Target.OnError(ex));
                    return;
                }

                if (result != null)
                {
                    Callback(() => Target.OnNext(result));
                }
                if (msg == WM_DESTROY)
                {
                    Callback(() =>
                    {
                        Target.OnCompleted();
                        Dispose();
                    });
                }
            }

            private void Callback(Action action)
            {
                Parent.QueueCallback(() =>
                {
                    try
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Exception on hook callback for {0:x8}:\r\n{1}", HWnd.ToInt32(), ex);
                            Target.OnError(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unhandled exception on hook callback for {0:x8}.  Destroying.\r\n{1}", HWnd.ToInt32(), ex);
                        Dispose();
                    }
                });
            }

            public void Dispose()
            {
                // RemoveRegistration is idempotent, we don't need to have IsDisposed.
                Parent.RemoveRegistration(this);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CWPSTRUCT
        {
            public IntPtr lParam;
            public IntPtr wParam;
            public int message;
            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CWPRETSTRUCT
        {
            public IntPtr lResult;
            public IntPtr lParam;
            public IntPtr wParam;
            public int message;
            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            public int pt_x;
            public int pt_y;
        }

        private sealed class HookLifetimeManager : IDisposable
        {
            // we must keep a reference to the delegate since we are passing it to SetWindowsHookEx
            private readonly GetOrSendWndProc pGetMessageHook_Callback;
            private readonly GetOrSendWndProc pSendMessageHook_Callback;
            private bool IsDisposed;
            private readonly HookSafeHandle GetMessageHook;
            private readonly HookSafeHandle SendMessageHook;
            private readonly HookCallback Callback;

            // ctor runs from within a lock, and should be as quick as is possible
            public HookLifetimeManager(HookCallback callback)
            {
                Contract.Requires(callback != null);

                this.pGetMessageHook_Callback = GetMessageHook_Callback;
                this.pSendMessageHook_Callback = SendMessageHook_Callback;
                this.Callback = callback;

                this.GetMessageHook = SetWindowsHookEx(WH_GETMESSAGE, pGetMessageHook_Callback, IntPtr.Zero, GetCurrentThreadId());
                if (this.GetMessageHook.IsInvalid)
                {
                    throw new Win32Exception();
                }
                try
                {
                    this.SendMessageHook = SetWindowsHookEx(WH_CALLWNDPROCRET, pSendMessageHook_Callback, IntPtr.Zero, GetCurrentThreadId());
                    if (this.SendMessageHook.IsInvalid)
                    {
                        throw new Win32Exception();
                    }
                }
                catch
                {
                    GetMessageHook.Dispose();
                    throw;
                }
            }

            // this is run from within a lock, and should be as short as is possible.
            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }
                IsDisposed = true;

                try
                {
                    GetMessageHook.Dispose();
                }
                finally
                {
                    SendMessageHook.Dispose();
                }
            }

            private IntPtr GetMessageHook_Callback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                IntPtr lResult;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    // cer
                }
                finally
                {
                    // call event handler
                    if (nCode == HC_ACTION && lParam != IntPtr.Zero)
                    {
                        try
                        {
                            var st = Marshal.PtrToStructure<MSG>(lParam);
                            //Debug.WriteLine("HC_ACTION hwnd: {0:x8} msg {1:x4} wp {2:x8} lp {3:x8}",
                            //    st.hwnd.ToInt32(), st.message, st.wParam.ToInt32(), st.lParam.ToInt32());
                            Callback(st.hwnd, st.message, st.wParam, st.lParam, IntPtr.Zero);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Exception in GetMessageHook_Callback: " + ex.ToString());
                        }
                    }

                    if (!IsDisposed)
                    {
                        // If this were called when disposed, this would throw a "Safe handle is closed" exception
                        lResult = CallNextHookEx(GetMessageHook, nCode, wParam, lParam);
                    }
                    else
                    {
                        lResult = IntPtr.Zero;
                    }
                }

                return lResult;
            }

            private IntPtr SendMessageHook_Callback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                IntPtr lResult;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    // cer
                }
                finally
                {
                    // call event handler
                    if (nCode == HC_ACTION && lParam != IntPtr.Zero)
                    {
                        try
                        {
                            var st = Marshal.PtrToStructure<CWPRETSTRUCT>(lParam);
                            //Debug.WriteLine("HC_ACTION hwnd: {0:x8} msg {1:x4} wp {2:x8} lp {3:x8}",
                            //    st.hwnd.ToInt32(), st.message, st.wParam.ToInt32(), st.lParam.ToInt32());
                            Callback(st.hwnd, st.message, st.wParam, st.lParam, st.lResult);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Exception in SendMessageHook_Callback: " + ex.ToString());
                        }
                    }

                    if (!IsDisposed)
                    {
                        // If this were called when disposed, this would throw a "Safe handle is closed" exception
                        lResult = CallNextHookEx(GetMessageHook, nCode, wParam, lParam);
                    }
                    else
                    {
                        lResult = IntPtr.Zero;
                    }
                }

                return lResult;
            }
        }
    }
}
