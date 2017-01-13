// these have to be present, since the System.Diagnostics.Debug 
// class is marked [Conditional("DEBUG")] and the Trace class is
// marked with [Conditional("TRACE")] 
#define TRACE
#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook
{
    using Esatto.Win32.Com;
    using IPC;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using static NativeMethods;

    public sealed class ForeignMdiWindow : IDisposable
    {
        private IHookedProcess Hook;
        private IHookedMdiWindow ForeignWindow;
        
        private ForeignMdiWindow(IntPtr hWndMdiClient, IHookedProcess hook)
        {
            Contract.Requires(hook != null);

            this.Hook = hook;
            this.ForeignWindow = hook.GetMdiWindow(hWndMdiClient);
        }

        ~ForeignMdiWindow()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                ForeignWindow.Dispose();
            }
            catch (Exception ex) when (!disposing)
            {
                Debug.WriteLine($"Exception on finalizer\r\n{ex}");
            }
        }

        public static ForeignMdiWindow Get(Process target)
        {
            var mainWindow = target.MainWindowHandle;
            var hMdiClient = FindWindowEx(mainWindow, IntPtr.Zero, "MDIClient", null);
            if (hMdiClient == IntPtr.Zero)
            {
                throw new InvalidOperationException("MDIClient not found");
            }

            return Get(hMdiClient);
        }

        public static ForeignMdiWindow Get(IntPtr hWndMdiClient)
        {
            int processId;
            GetWindowThreadProcessId(hWndMdiClient, out processId);
            if (processId == 0)
            {
                throw new Win32Exception();
            }
            var moniker = IpcConstants.GetRotMonikerName(processId);
            var hook = GetHookedProcess(hWndMdiClient, moniker);

            return new ForeignMdiWindow(hWndMdiClient, hook);
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

        public void ShowWindow(SurrogatedMdiChild child)
        {
            Contract.Requires(child != null);

            child.Attach(ForeignWindow);
        }
    }
}
