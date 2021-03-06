﻿using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook
{
    public class SurrogatedMdiChild : Control
    {
        private Size _SurrogateSize;
        private Point _SurrogateLocation;
        private bool _IsResizable;
        private bool _IsMovable;
        private bool _IsVisible;
        private string _Title;
        private ISurrogateMdiChild Surrogate;
        private SurrogatedMdiChildContentProxy Proxy;

        public SurrogatedMdiChild()
        {
            this.Proxy = new SurrogatedMdiChildContentProxy(this);

            this.Dock = DockStyle.Fill;
            this.SurrogateSize = new Size(400, 300);
            this._IsVisible = true;
            this._IsResizable = true;
            this._IsMovable = true;
            this._Title = "Foreign Content";
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;

                // we may be creating before attach, and in that case we will SetParent later
                if (Surrogate != null)
                {
                    cp.Parent = Surrogate.ContentPanelHandle;
                }

                return cp;
            }
        }

        public Size SurrogateSize
        {
            get { return _SurrogateSize; }
            set
            {
                _SurrogateSize = value;
                this.Size = value;

                if (Surrogate != null)
                {
                    Surrogate.Size = value;
                }
            }
        }

        public Point SurrogateLocation
        {
            get { return _SurrogateLocation; }
            set
            {
                _SurrogateLocation = value;

                if (Surrogate != null)
                {
                    Surrogate.Location = value;
                }
            }
        }

        public override string Text
        {
            get { return _Title; }
            set
            {
                base.Text = value;
                _Title = value;

                if (Surrogate != null)
                {
                    Surrogate.Title = value;
                }
            }
        }

        public bool SurrogateVisible
        {
            get { return _IsVisible; }
            set
            {
                _IsVisible = value;

                if (Surrogate != null)
                {
                    Surrogate.IsVisible = value;
                }
            }
        }

        public bool IsResizable
        {
            get { return _IsResizable; }
            set
            {
                AssertNotCreated();

                _IsResizable = value;
            }
        }

        public bool IsMovable
        {
            get { return _IsMovable; }
            set
            {
                AssertNotCreated();

                _IsMovable = value;
            }
        }

        private void AssertCreated()
        {
            if (Surrogate == null)
            {
                throw new InvalidOperationException("Window must be attached");
            }
        }

        internal void Attach(IHookedMdiWindow foreignWindow)
        {
            Contract.Requires(foreignWindow != null);

            this.AssertNotCreated();
            Surrogate = foreignWindow.CreateChild(Text, new Rectangle(SurrogateLocation, SurrogateSize), IsResizable, IsMovable, Proxy);
            if (IsHandleCreated)
            {
                this.SetParent(Surrogate.ContentPanelHandle);
            }
            else
            {
                this.CreateControl();
            }
            this.Size = Surrogate.Size;
        }

        private void AssertNotCreated()
        {
            if (Surrogate != null)
            {
                throw new InvalidOperationException("Illegal change after attaching surrogate");
            }
        }

        public void Close()
        {
            AssertCreated();

            Surrogate.Close();
        }

        protected virtual void OnClosing()
        {
            // no-op, may be overridden
        }

        private void OnDestroy()
        {
            Surrogate = null;
        }

        private sealed class SurrogatedMdiChildContentProxy : StandardOleMarshalObject, ISurrogateMdiChildContent
        {
            private SurrogatedMdiChild SurrogateContent;

            public SurrogatedMdiChildContentProxy(SurrogatedMdiChild content)
            {
                Contract.Requires(content != null);

                this.SurrogateContent = content;
            }

            public void Close()
            {
                var content = SurrogateContent;
                if (content != null)
                {
                    content.OnClosing();
                    content.OnDestroy();
                    SurrogateContent = null;
                }
            }

            public void Ping()
            {
                // no-op
            }

            public void SizeChanged(Size newSize)
            {
                var sc = SurrogateContent;
                if (sc != null)
                {
                    sc.Size = newSize;
                }
            }
        }
    }
}
