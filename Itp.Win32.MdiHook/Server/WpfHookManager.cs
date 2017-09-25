using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Visual = System.Windows.Media.Visual;

namespace Itp.Win32.MdiHook.Server
{
    internal static class WpfHookManager
    {
        public static IWpfWindowHook Hook(WpfHookRegistrationRecord record, IDcsMarshalledObserver observer)
        {
            // get visual
            var hwndSource = HwndSource.FromHwnd(record.HWnd);
            if (hwndSource == null)
            {
                throw new InvalidOperationException($"HwndSource 0x{record.HWnd.ToInt32():x8} not found");
            }
            var rootVisual = hwndSource.RootVisual;
            if (rootVisual == null)
            {
                throw new InvalidOperationException("RootVisual is null");
            }

            // get result source
            var resultType = record.ResultType;
            var tObserver = typeof(IObserver<>).MakeGenericType(resultType);
            var tSource = typeof(DcsMarshalledProxy<>).MakeGenericType(resultType);
            var tSourceCtor = tSource.GetConstructor(new[] { typeof(IDcsMarshalledObserver) });
            var resultSource = tSourceCtor.Invoke(new object[] { observer });

            // instantiate hook
            var tHandler = record.HookType;
            var tParam = record.ParameterType;

            if (tParam == null)
            {
                var ctor = tHandler.GetConstructor(new[] { typeof(Visual), tObserver });
                if (ctor != null)
                {
                    return (IWpfWindowHook)ctor.Invoke(new object[] { rootVisual, resultSource });
                }
            }
            else
            {
                var ctor = tHandler.GetConstructor(new[] { typeof(Visual), tObserver, tParam });
                if (ctor != null)
                {
                    return (IWpfWindowHook)ctor.Invoke(new object[] { rootVisual, resultSource, record.Parameter });
                }
            }

            throw new NotSupportedException($"No suitable constructor found.  Should be .ctor(System.Windows.Media.Visual, {tObserver.FullName}, {tParam.FullName})");
        }
    }
}
