using Esatto.Win32.Com;
using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook.Server
{
    // runs in-proc to the hooked process
    internal sealed class HookedProcess : StandardOleMarshalObject, IHookedProcess
    {
        private static readonly object SyncCreateInstance = new object();
        public static HookedProcess Instance { get; private set; }
        private static RotRegistration RotRegistration;
        private HookManager HookManager;

        public HookedProcess()
        {
            this.HookManager = new HookManager();
        }

        internal static HookedProcess GetInstance()
        {
            lock (SyncCreateInstance)
            {
                if (Instance != null)
                {
                    return Instance;
                }

                Instance = new HookedProcess();
                RotRegistration = new RotRegistration(IpcConstants.GetRotMonikerName(Process.GetCurrentProcess().Id), Instance);

                return Instance;
            }
        }

        public IHookedMdiWindow GetMdiWindow(IntPtr hWndMdiClient)
        {
            var newWindow = new HookedMdiWindow(hWndMdiClient);
            return newWindow.GetProxy();
        }

        public IWindowHook RegisterHook(HookRegistrationRecord hook, IDcsMarshalledObserver observer)
        {
            return HookManager.AddRegistration(hook, observer);
        }

        public IWpfWindowHook RegisterWpfHook(WpfHookRegistrationRecord hook, IDcsMarshalledObserver observer)
        {
            return WpfHookManager.Hook(hook, observer);
        }
    }
}
