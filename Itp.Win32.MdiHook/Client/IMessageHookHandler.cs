using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook
{
    public interface IMessageHookHandler<out TResult>
        where TResult : class
    {
        TResult HandleMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr lResult);
    }
}
