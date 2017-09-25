using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Itp.Win32.MdiHook.DemoClient
{
    public partial class Form1 : Form
    {
        ForeignProcess wpfWindow;
        ForeignMdiWindow window;
        IntPtr notepad;
        IntPtr wpfWindowIntPtr;

        public Form1()
        {
            InitializeComponent();

            var demoProcess = Process.GetProcessesByName("Itp.Win32.DemoMdiApplication").SingleOrDefault();
            if (demoProcess != null)
            {
                window = ForeignMdiWindow.Get(demoProcess);
            }
            else
            {
                foreach (var a in this.Controls.Cast<Control>())
                {
                    if (a == btHookNotepad
                        || a == btHookWpf)
                    {
                        // no-op
                    }
                    else
                    {
                        a.Enabled = false;
                    }
                }
            }

            var demoWpfProcess = Process.GetProcessesByName("Itp.Win32.DemoWpfTarget").SingleOrDefault();
            if (demoWpfProcess != null)
            {
                wpfWindow = ForeignProcess.Get(demoWpfProcess);
                wpfWindowIntPtr = demoWpfProcess.MainWindowHandle;
            }
            else
            {
                btHookWpf.Enabled = false;
            }
        }

        MdiWindowSample last;

        private void button1_Click(object sender, EventArgs e)
        {
            window.ShowWindow(last = new MdiWindowSample()
            {
                SurrogateLocation = new Point(100, 100),
                SurrogateSize = new Size(250, 600)
            });
            btCloseLast.Enabled = true;
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

        private void btCloseLast_Click(object sender, EventArgs e)
        {
            last.Close();
            btCloseLast.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            window.ShowWindow(new MdiWindowSample() { IsMovable = false });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (notepad == IntPtr.Zero)
            {
                var proc = Process.Start(@"c:\windows\syswow64\notepad.exe");

                System.Threading.Thread.Sleep(200);
                var mw = proc.MainWindowHandle;
                notepad = FindWindowEx(mw, IntPtr.Zero, "Edit", null);
            }
            var messageLog = new MessageLogForm(notepad);
            messageLog.Show();
        }

        const string User32 = "user32.dll";

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr childAfter, string className, string windowTitle);

        private void btHookWpf_Click(object sender, EventArgs e)
        {
            var newWindow = new WpfLogForm(wpfWindow, wpfWindowIntPtr);
            newWindow.Owner = this;
            newWindow.Show();
        }
    }
}
