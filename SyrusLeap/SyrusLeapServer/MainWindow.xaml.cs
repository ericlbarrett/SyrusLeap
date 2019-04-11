using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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

namespace SyrusLeapServer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        ServerBTManager sbm;
        LeapManager lpm;

        Timer timer;

        public MainWindow() {
            InitializeComponent();
     
            sbm = new ServerBTManager();
            sbm.Initialize();

            lpm = new LeapManager();
            lpm.Initialize();

            timer = new Timer(50 / 3);
            timer.Elapsed += new ElapsedEventHandler(SendFrame);
            timer.Start();
        }

        private void SendFrame(object source, ElapsedEventArgs e) {
            if (sbm.GetConnectedStatus()) {
                SyrusPacket pak = new SyrusPacket();
                pak.id = 20;
                Dispatcher.BeginInvoke(new Action(() => {
                    Leap.Vector v = lpm.GetHandPosition();
                    Text.Text = "X: " + v.x + " Y: " + v.y + " Z: " + v.z;
                }), DispatcherPriority.SystemIdle);
                

                byte[] arr = lpm.getFrameInfo();
                pak.data = arr;
                pak.n = (byte)pak.data.Length;

                sbm.SendPacket(pak);
            }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e) {
            SyrusPacket pak = new SyrusPacket();
            pak.id = 24;

            pak.data = System.Text.Encoding.ASCII.GetBytes(TextBox.Text);
            pak.n = (byte)pak.data.Length;

            sbm.SendPacket(pak);
        }
    }
}
