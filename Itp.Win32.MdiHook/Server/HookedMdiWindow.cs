using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics.Contracts;

namespace Itp.Win32.MdiHook.Server
{
    // runs in-proc to the hooked process
    internal sealed class HookedMdiWindow : IDisposable
    {
        // we need to synchronize this list since we may be called from
        // the finalizer thread via HookedMdiWindowProxy..dtor
        private readonly object syncSurrogatedChildren;
        private List<SurrogateMdiChild> SurrogatedChildren;

        public IntPtr MdiClientHwnd { get; }

        public HookedMdiWindow(IntPtr hWndMdiClient)
        {
            this.syncSurrogatedChildren = new object();
            this.SurrogatedChildren = new List<SurrogateMdiChild>();

            this.MdiClientHwnd = hWndMdiClient;
        }

        public void Dispose()
        {
            lock (syncSurrogatedChildren)
            {
                var firstChild = SurrogatedChildren.FirstOrDefault();
                if (firstChild == null)
                {
                    return;
                }

                firstChild.Surrogate.Invoke(new Action(DestroyAllForms));
            }
        }

        private void DestroyAllForms()
        {
            lock (syncSurrogatedChildren)
            {
                foreach (var child in SurrogatedChildren)
                {
                    child.ForceClose();
                }

                SurrogatedChildren.Clear();
            }
        }

        public IHookedMdiWindow GetProxy()
        {
            return new HookedMdiWindowProxy(this);
        }

        // this is used to detect when Release is called.  We cannot keep a reference to
        // this class or it won't work.  We can't use HookedMdiWindow itself since it has
        // a gcroot through SurrogateMdiChild.
        private class HookedMdiWindowProxy : StandardOleMarshalObject, IHookedMdiWindow
        {
            private readonly HookedMdiWindow Parent;

            public HookedMdiWindowProxy(HookedMdiWindow parent)
            {
                Contract.Requires(parent != null);

                this.Parent = parent;
            }

            ~HookedMdiWindowProxy()
            {
                this.Parent.Dispose();
            }

            public void Dispose()
            {
                this.Parent.Dispose();
            }

            public ISurrogateMdiChild CreateChild(string title, Rectangle location, bool isResizable, ISurrogateMdiChildContent childContent)
            {
                return Parent.CreateChild(title, location, isResizable, childContent);
            }
        }

        public ISurrogateMdiChild CreateChild(string title, Rectangle location, bool isResizable, ISurrogateMdiChildContent childContent)
        {
            lock (syncSurrogatedChildren)
            {
                var newChild = new SurrogateMdiChild(this, title, location, isResizable, childContent);
                SurrogatedChildren.Add(newChild);
                return newChild;
            }
        }

        public void NotifyClosed(SurrogateMdiChild child)
        {
            Contract.Requires(child != null);

            lock (syncSurrogatedChildren)
            {
                SurrogatedChildren.Remove(child);
            }
        }
    }
}
