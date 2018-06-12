using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook.DemoClient
{
    public partial class SizeContentSample : SurrogatedMdiChild
    {
        public SizeContentSample()
        {
            InitializeComponent();
        }

        private void SizeContentSample_SizeChanged(object sender, EventArgs e)
        {
            this.label1.Text = Size.ToString();
        }
    }
}
