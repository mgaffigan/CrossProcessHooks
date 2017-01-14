using Esatto.Win32.Com;
using static Itp.Win32.MdiHook.NativeMethods;
using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Itp.Win32.MdiHook
{
    public sealed class ForeignProcess
    {
        private readonly IHookedProcess Hook;
        private readonly Process Process;

        private ForeignProcess(IHookedProcess hook, Process proc)
        {
            Contract.Requires(hook != null);
            Contract.Requires(proc != null);

            this.Hook = hook;
            this.Process = proc;
        }

        public ForeignMdiWindow GetMdiWindow(IntPtr hMdiClient)
        {
            return new ForeignMdiWindow(hMdiClient, Hook);
        }

        public ForeignMdiWindow GetMdiWindow()
        {
            var mainWindow = Process.MainWindowHandle;
            var hMdiClient = FindWindowEx(mainWindow, IntPtr.Zero, "MDIClient", null);
            if (hMdiClient == IntPtr.Zero)
            {
                throw new InvalidOperationException("MDIClient not found");
            }

            return GetMdiWindow(hMdiClient);
        }

        public static ForeignProcess Get(Process target)
        {
            return Get(target.MainWindowHandle);
        }

        public static ForeignProcess Get(IntPtr hWnd)
        {
            int processId;
            GetWindowThreadProcessId(hWnd, out processId);
            if (processId == 0)
            {
                throw new Win32Exception();
            }
            var moniker = IpcConstants.GetRotMonikerName(processId);
            var hook = GetHookedProcess(hWnd, moniker);

            return new ForeignProcess(hook, Process.GetProcessById(processId));
        }

        private static IHookedProcess GetHookedProcess(IntPtr hWndTarget, string moniker)
        {
            var createMutex = new Mutex(initiallyOwned: false, name: moniker);

            // get the mutex
            try
            {
                createMutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
                // no-op, still received mutex
            }
            try
            {
                // check if it is already installed
                try
                {
                    return (IHookedProcess)RotRegistration.GetRegisteredObject(moniker);
                }
                catch (KeyNotFoundException)
                {
                    // no-op, we need to inject
                }

                // install
                Injector.InstallHook(hWndTarget, typeof(Server.MdiHookTarget), null);
                return (IHookedProcess)RotRegistration.GetRegisteredObject(moniker);
            }
            finally
            {
                createMutex.ReleaseMutex();
            }
        }

        public ForeignHook<T> HookWindow<T>(IntPtr hWnd, Type tHandler, object oParam = null)
            where T : class
        {
            Contract.Requires(tHandler != null);
            Contract.Requires(typeof(IMessageHookHandler<T>).IsAssignableFrom(tHandler));

            var source = new DcsMarshalledSource<T>();
            var record = HookRegistrationRecord.Create(hWnd, tHandler, typeof(T), oParam);
            var rh = Hook.RegisterHook(record, source.ComProxy);
            return new ForeignHook<T>(rh, source);
        }
    }
}
