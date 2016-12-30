using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook.IPC
{
    // public for regAsm
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISurrogateMdiChild
    {
        IntPtr ContentPanelHandle { get; }

        string Title { get; set; }

        bool IsVisible { get; set; }

        Size Size { get; set; }

        Point Location { get; set; }

        void Close();
    }
}
