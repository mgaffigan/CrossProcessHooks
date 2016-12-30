using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics.Contracts;

namespace Itp.Win32.MdiHook.Server
{
    using System.Diagnostics;
    using static NativeMethods;

    // runs in-proc to the hooked process
    internal sealed class SurrogateMdiChild : StandardOleMarshalObject, ISurrogateMdiChild
    {
        private HookedMdiWindow Parent;
        private bool IsResizable;
        private ISurrogateMdiChildContent ChildContent;

        public SurrogateMdiChildControl Surrogate { get; }

        public IntPtr ContentPanelHandle => Surrogate.Handle;

        public string Title
        {
            get { return Surrogate.Text; }
            set { Surrogate.Text = value; }
        }

        public bool IsVisible
        {
            get { return Surrogate.Visible; }
            set { Surrogate.Visible = value; }
        }

        public Size Size
        {
            get { return Surrogate.ClientSize; }
            set { Surrogate.ClientSize = value; }
        }

        public Point Location
        {
            get { return Surrogate.Location; }
            set { Surrogate.Location = value; }
        }

        public SurrogateMdiChild(HookedMdiWindow parent, string title, Rectangle location, bool isResizable, ISurrogateMdiChildContent childContent)
        {
            Contract.Requires(parent != null);
            Contract.Requires(childContent != null);

            this.Parent = parent;
            this.IsResizable = isResizable;
            this.ChildContent = childContent;

            this.Surrogate = new SurrogateMdiChildControl(this);
            this.Surrogate.Text = title;
            this.Surrogate.ClientSize = location.Size;
            this.Surrogate.Location = location.Location;
            this.Surrogate.CreateControl();
        }

        public sealed class SurrogateMdiChildControl : Control
        {
            private SurrogateMdiChild Proxy;
            private readonly Stopwatch stpLastPing;

            public SurrogateMdiChildControl(SurrogateMdiChild parent)
            {
                Contract.Requires(parent != null);

                this.Proxy = parent;
                this.stpLastPing = Stopwatch.StartNew();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.Parent = this.Proxy.Parent.MdiClientHwnd;
                    cp.ExStyle = WS_EX_MDICHILD | WS_EX_WINDOWEDGE | WS_EX_APPWINDOW;
                    cp.Style = WS_OVERLAPPEDWINDOW | WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN;
                    if (!this.Proxy.IsResizable)
                    {
                        // remote the flags for the minimize and maximize buttons
                        // ThickFrame represents the ability to resize by dragging the 
                        // frame
                        cp.Style &= ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_THICKFRAME);
                    }
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_CLOSE)
                {
                    try
                    {
                        this.Proxy.ChildContent.Close();
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception while calling cross process MDI Close handler");
                        Debug.WriteLine(ex);
                        // no-op, allow the close
                    }

                    this.Proxy.Parent.NotifyClosed(this.Proxy);
                }
                else if (m.Msg == WM_EXITSIZEMOVE)
                {
                    try
                    {
                        this.Proxy.ChildContent.SizeChanged(ClientSize);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception while calling cross process MDI size changed handler");
                        Debug.WriteLine(ex);
                    }
                }
                else if (m.Msg == WM_ERASEBKGND && stpLastPing.Elapsed > TimeSpan.FromMilliseconds(100))
                {
                    try
                    {
                        this.Proxy.ChildContent.Ping();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Ping failed, closing form");
                        Debug.WriteLine(ex);

                        this.Visible = false;
                    }

                    stpLastPing.Restart();
                }

                base.WndProc(ref m);
            }
        }

        public void ForceClose()
        {
            this.Surrogate.Visible = false;
        }

        public void Close()
        {
            ForceClose();
        }
    }
}
