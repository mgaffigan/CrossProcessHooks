// these have to be present, since the System.Diagnostics.Debug 
// class is marked [Conditional("DEBUG")] and the Trace class is
// marked with [Conditional("TRACE")] 
#define TRACE
#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Itp.Win32.MdiHook.IPC
{
    // public for regAsm
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDcsMarshalledObserver
    {
        void OnNext(string tData);

        void OnCompleted();

        void OnError(string message);
    }

    public sealed class DcsMarshalledSource<T> : IObservable<T>, IDisposable
            where T : class
    {
        private readonly Subject<T> Source;
        private readonly DcsMarshalledObserverProxy Proxy;

        public IDcsMarshalledObserver ComProxy => Proxy;

        internal bool IsComplete { private set; get; }

        public DcsMarshalledSource()
        {
            this.Source = new Subject<T>();
            this.Proxy = new DcsMarshalledObserverProxy(this);
        }

        public void Dispose()
        {
            this.Proxy.Disconnect();
        }

        private class DcsMarshalledObserverProxy : IDcsMarshalledObserver
        {
            private readonly DataContractSerializer dcs;
            private DcsMarshalledSource<T> Target;

            public DcsMarshalledObserverProxy(DcsMarshalledSource<T> tImpl)
            {
                Contract.Requires(tImpl != null);

                this.dcs = new DataContractSerializer(typeof(T));
                this.Target = tImpl;
            }

            public void Disconnect()
            {
                this.Target = null;
            }

            public void OnCompleted()
            {
                var tgt = Target;
                if (tgt != null)
                {
                    tgt.IsComplete = true;
                }
                Target?.Source.OnCompleted();
            }

            public void OnError(string message)
            {
                Target?.Source.OnError(new InvalidOperationException(message));
            }

            public void OnNext(string tData)
            {
                if (tData == null)
                {
                    Target?.Source.OnNext(null);
                }
                else
                {
                    using (var sr = new StringReader(tData))
                    using (var xr = XmlDictionaryReader.CreateDictionaryReader(XmlReader.Create(sr)))
                    {
                        Target?.Source.OnNext((T)dcs.ReadObject(xr));
                    }
                }
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (IsComplete)
            {
                throw new ObjectDisposedException("Source has disconnected");
            }

            return Source.Subscribe(observer);
        }
    }

    internal class DcsMarshalledProxy
    {
        private readonly DataContractSerializer dcs;
        private readonly Type T;
        private readonly IDcsMarshalledObserver Target;

        public DcsMarshalledProxy(IDcsMarshalledObserver target, Type t)
        {
            Contract.Requires(target != null);
            Contract.Requires(t != null);

            this.dcs = new DataContractSerializer(t);
            this.T = t;
            this.Target = target;
        }

        public void OnCompleted()
        {
            Target.OnCompleted();
        }

        public void OnError(Exception exception)
        {
            Target.OnError(exception.ToString());
        }

        public void OnNext(object tData)
        {
            if (tData == null)
            {
                Target.OnNext(null);
            }
            else
            {
                using (var sw = new StringWriter())
                using (var xw = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(sw)))
                {
                    dcs.WriteObject(xw, tData);
                    xw.Flush();

                    Target.OnNext(sw.ToString());
                }
            }
        }
    }

    internal sealed class DcsMarshalledProxy<TResult> : DcsMarshalledProxy, IObserver<TResult>
        where TResult : class
    {
        public DcsMarshalledProxy(IDcsMarshalledObserver target) 
            : base(target, typeof(TResult))
        {
        }

        public void OnNext(TResult value)
        {
            base.OnNext(value);
        }
    }
}
