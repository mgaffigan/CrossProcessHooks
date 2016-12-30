using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook.IPC
{
    internal static class IpcConstants
    {
        public static string GetRotMonikerName(int processId)
        {
            return $"ItpMdiHook_{processId:x8}";
        }
    }
}
