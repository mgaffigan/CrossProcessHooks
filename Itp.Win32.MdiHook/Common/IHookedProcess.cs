using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook.IPC
{
    // public for regAsm
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IHookedProcess
    {
        IHookedMdiWindow GetMdiWindow(IntPtr hWndMdiClient);
    }
}
