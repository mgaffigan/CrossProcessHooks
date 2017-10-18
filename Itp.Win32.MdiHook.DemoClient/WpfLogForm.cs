using Itp.Win32.MdiHook.IPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Visual = System.Windows.Media.Visual;
using FrameworkElement = System.Windows.FrameworkElement;
using System.Reactive.Linq;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Itp.Win32.MdiHook.DemoClient
{
    public partial class WpfLogForm : Form
    {
        public WpfLogForm(ForeignProcess process, IntPtr hwnd)
        {
            InitializeComponent();

            this.Process = process;
            this.Hook = process.HookWpfWindow<WpfHookResult>(hwnd, typeof(TargetWpfHook), new WpfHookParam());
            this.Hook
                .ObserveOn(this)
                .Subscribe(
                    t => lbLog.Items.Add(t.Message),
                    () => Close());
            this.Hook.Invoke();
        }

        public ForeignProcess Process { get; private set; }
        internal ForeignWpfHook<WpfHookResult> Hook { get; private set; }
    }

    [DataContract(Namespace = "urn:foo")]
    class WpfHookResult
    {
        [DataMember]
        public string Message { get; set; }

        public WpfHookResult(string m)
        {
            this.Message = m;
        }
    }

    [DataContract(Namespace = "urn:foo")]
    class WpfHookParam
    {
    }

    class TargetWpfHook : IWpfWindowHook
    {
        private readonly IObserver<WpfHookResult> Target;
        private readonly WpfTextBox tbTime;
        private readonly WpfTextBox tbArbitrary;

        public TargetWpfHook(Visual visual, IObserver<WpfHookResult> target, WpfHookParam param)
        {
            this.Target = target;
            var frameworkElement = (FrameworkElement)visual;

            frameworkElement.Unloaded += (_1, _2) => target.OnCompleted();

            tbTime = (WpfTextBox)frameworkElement.FindName("tbTime");
            tbTime.TextChanged += tbTime_TextChanged;

            tbArbitrary = (WpfTextBox)frameworkElement.FindName("tbArbitrary");
            tbArbitrary.TextChanged += tbArbitrary_TextChanged;
        }

        private void tbArbitrary_TextChanged(object sender, EventArgs e)
        {
            Target.OnNext(new WpfHookResult($"tbArbitrary: {tbArbitrary.Text}"));
        }

        private void tbTime_TextChanged(object sender, EventArgs e)
        {
            Target.OnNext(new WpfHookResult($"tbTime: {tbTime.Text}"));
        }

        public void Dispose()
        {
            tbTime.TextChanged -= tbTime_TextChanged;
            tbArbitrary.TextChanged -= tbArbitrary_TextChanged;
        }

        public void Invoke()
        {
            tbTime_TextChanged(null, null);
            tbArbitrary_TextChanged(null, null);
        }
    }
}
