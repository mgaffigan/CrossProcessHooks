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

        private readonly SurrogateMdiChildControl Surrogate;

        IntPtr ISurrogateMdiChild.ContentPanelHandle => Surrogate.Handle;

        string ISurrogateMdiChild.Title
        {
            get { return Surrogate.Text; }
            set
            {
                Surrogate.AssertThread();
                Surrogate.Text = value;
            }
        }

        bool ISurrogateMdiChild.IsVisible
        {
            get { return Surrogate.Visible; }
            set
            {
                Surrogate.AssertThread();
                Surrogate.Visible = value;
            }
        }

        Size ISurrogateMdiChild.Size
        {
            get { return Surrogate.ClientSize; }
            set
            {
                Surrogate.AssertThread();
                Surrogate.ClientSize = value;
            }
        }

        Point ISurrogateMdiChild.Location
        {
            get { return Surrogate.Location; }
            set
            {
                Surrogate.AssertThread();
                Surrogate.Location = value;
            }
        }

        void ISurrogateMdiChild.Close()
        {
            Surrogate.Close();
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

            // for some reason, these are re-added despite CreateParams
            // so we have to remove them here
            var hwnd = this.Surrogate.Handle;
            var ws = GetWindowLong(hwnd, GWL_STYLE);
            ws &= ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            ws &= ~(IsResizable ? 0 : WS_THICKFRAME);
            SetWindowLong(hwnd, GWL_STYLE, ws);

            // since we changed the window styles, we have to redraw
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
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

                    cp.Style = WS_OVERLAPPEDWINDOW | WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN;
                    // get rid of WS_MINIMIZEBOX | WS_MAXIMIZEBOX since minimize is dumb, and maximize doesn't work
                    cp.Style &= ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                    // ThickFrame represents the ability to resize by dragging the border 
                    cp.Style &= ~(this.Proxy.IsResizable ? 0 : WS_THICKFRAME);

                    // no WS_EX_APPWINDOW since we don't show in taskbar
                    // WS_EX_DLGMODALFRAME for no icon
                    cp.ExStyle = WS_EX_MDICHILD | WS_EX_WINDOWEDGE | WS_EX_DLGMODALFRAME;
                    
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_SYSCHAR)
                {
                    // for some reason, we get a deadlock when Ctrl+Spacebar is pressed with a 
                    // foreign window focused.  That happens on processing of WM_SYSCHAR
                    return;
                }
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
                    // Something in Framework is using SendMessage, which does not play nicely with COM
                    catch (COMException cex) when (cex.HResult == Esatto.Win32.Com.NativeMethods.RPC_E_CANTCALLOUT_ININPUTSYNCCALL)
                    {
                        // no-op, we will ping next time around.
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

            public void Close()
            {
                AssertThread();

                SendMessage(new HandleRef(this, Handle), WM_CLOSE, 0, 0);
            }

            public void AssertThread()
            {
                if (InvokeRequired)
                {
                    throw new InvalidOperationException("Invalid cross thread access to SurrogateMdiChild");
                }
            }
        }

        // may be called from finalizer thread via HookedMdiWindow
        public void ForceClose()
        {
            this.Surrogate.BeginInvoke(new Action(() =>
            {
                this.Surrogate.Visible = false;
            }));
        }
    }
}
