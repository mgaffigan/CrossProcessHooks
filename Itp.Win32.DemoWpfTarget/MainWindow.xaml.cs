using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Itp.Win32.DemoWpfTarget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Timer = new DispatcherTimer();
            this.Timer.Interval = TimeSpan.FromSeconds(1);
            this.Timer.Tick += Timer_Tick;
            this.Timer.IsEnabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.tbTime.Text = DateTime.Now.ToString("u");
        }

        public DispatcherTimer Timer { get; }
    }
}
