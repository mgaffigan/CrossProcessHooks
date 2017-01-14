using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itp.Win32.MdiHook
{
    public sealed class ForeignHook<T> : IObservable<T>, IDisposable
        where T : class
    {
        private readonly IWindowHook Hook;
        private readonly DcsMarshalledSource<T> Source;

        public ForeignHook(IWindowHook hook, DcsMarshalledSource<T> source)
        {
            Contract.Requires(hook != null);
            Contract.Requires(source != null);

            this.Hook = hook;
            this.Source = source;
        }

        public void Dispose()
        {
            Source.Dispose();
            if (!Source.IsComplete)
            {
                Hook.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Source.Subscribe(observer);
        }
    }
}
