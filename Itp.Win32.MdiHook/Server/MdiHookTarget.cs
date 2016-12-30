using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook.Server
{
    // runs in-proc to the hooked process
    // entrypoint for Itp.Win32.MdiHook.Injector
    internal sealed class MdiHookTarget
    {
        public MdiHookTarget()
        {
            HookedProcess.GetInstance();
        }
    }
}
