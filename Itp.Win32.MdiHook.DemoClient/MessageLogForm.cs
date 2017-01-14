using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook.DemoClient
{
    public partial class MessageLogForm : Form
    {
        ForeignProcess proc;
        ForeignHook<string> hookResults;
        private readonly IntPtr Target;

        public MessageLogForm(IntPtr target)
        {
            InitializeComponent();

            this.Target = target;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            proc = ForeignProcess.Get(Target);
            hookResults = WMKeyDownHook.Get(proc, Target);
            hookResults
                .ObserveOn(this)
                .Subscribe(
                    t => listBox1.Items.Add(t), 
                    () => Close()
                );
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            hookResults.Dispose();

            base.OnClosing(e);
        }

        private class WMKeyDownHook : IMessageHookHandler<string>
        {
            public string HandleMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr lResult)
            {
                if (msg != 0x101 /* WM_KEYUP */)
                {
                    return null;
                }

                return ((Keys)wParam.ToInt32()).ToString();
            }

            public static ForeignHook<string> Get(ForeignProcess process, IntPtr hwnd)
            {
                return process.HookWindow<string>(hwnd, typeof(WMKeyDownHook));
            }
        }
    }
}
