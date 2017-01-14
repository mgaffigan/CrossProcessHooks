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
    using IPC;
    using System.Diagnostics;

    public sealed class ForeignMdiWindow : IDisposable
    {
        private IHookedProcess Hook;
        private IHookedMdiWindow ForeignWindow;
        
        internal ForeignMdiWindow(IntPtr hWndMdiClient, IHookedProcess hook)
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
            => ForeignProcess.Get(target).GetMdiWindow();

        public static ForeignMdiWindow Get(IntPtr hWndMdiClient) 
            => ForeignProcess.Get(hWndMdiClient).GetMdiWindow(hWndMdiClient);

        public void ShowWindow(SurrogatedMdiChild child)
        {
            Contract.Requires(child != null);

            child.Attach(ForeignWindow);
        }
    }
}
