using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook.DemoClient
{
    public partial class Form1 : Form
    {
        ForeignMdiWindow window;

        public Form1()
        {
            InitializeComponent();

            var demoProcess = Process.GetProcessesByName("Itp.Win32.DemoMdiApplication").Single();
            window = ForeignMdiWindow.Get(demoProcess);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            window.ShowWindow(new MdiWindowSample());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            window.ShowWindow(new MdiWindowSample() { IsResizable = false });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var child = new MdiWindowSample() { IsResizable = false };
            child.SurrogateLocation = new Point(0, 0);
            window.ShowWindow(child);

            var tmrMove = new Timer();
            var stpLocation = Stopwatch.StartNew();
            tmrMove.Tick += (_1, _2) =>
            {
                var theta = (stpLocation.Elapsed.TotalMinutes % 1) * 2 * Math.PI;
                var xLoc = Math.Sin(theta) * 200 + 200;
                var yLoc = Math.Cos(theta) * 200 + 200;

                try
                {
                    child.SurrogateLocation = new Point((int)xLoc, (int)yLoc);
                }
                catch
                {
                    tmrMove.Enabled = false;
                    MessageBox.Show("Failed to move");
                }
            };
            tmrMove.Interval = 1000 / 24;
            tmrMove.Enabled = true;
        }
    }
}
