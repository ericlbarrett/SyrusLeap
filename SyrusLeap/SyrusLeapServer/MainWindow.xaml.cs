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
using System.Reflection;
using Leap;

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
            lpm.OnGesture += GestureHappened;
            lpm.Initialize();

            timer = new Timer(1000 / 60);
            timer.Elapsed += new ElapsedEventHandler(SendFrame);
            timer.Start();

            try {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                if (key.GetValue(curAssembly.GetName().Name) != null) {
                    Startup.IsChecked = true;
                } else {
                    Startup.IsChecked = false;
                }
            } catch { }

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

        private void GestureHappened(Leap.Frame frame) {
            foreach (Gesture gesture in frame.Gestures()) {
                if (gesture.Type == Gesture.GestureType.TYPE_SCREEN_TAP) {
                    ScreenTapGesture screentapGesture = new ScreenTapGesture(gesture);
                    SyrusPacket pak = new SyrusPacket();
                    pak.id = 21;
                    pak.data = new byte[24];

                    lpm.ToBytes(screentapGesture.Position, pak.data, 0);
                    lpm.ToBytes(screentapGesture.Direction, pak.data, 12);

                    pak.n = (byte)pak.data.Length;
                    sbm.SendPacket(pak);
                }


            }

        }

        private void SendBtn_Click(object sender, RoutedEventArgs e) {
            SyrusPacket pak = new SyrusPacket();
            pak.id = 24;

            pak.data = System.Text.Encoding.ASCII.GetBytes(TextBox.Text);
            pak.n = (byte)pak.data.Length;

            System.Diagnostics.Debug.WriteLine("Send msg: " + TextBox.Text);

            sbm.SendPacket(pak);
        }

        private void Startup_Checked(object sender, RoutedEventArgs e) {
            try {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                //System.Diagnostics.Debug.WriteLine(curAssembly.Location);
                key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
            } catch { }
        }

        private void Startup_Unchecked(object sender, RoutedEventArgs e) {
            try {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                if (key.GetValue(curAssembly.GetName().Name) != null) {
                    key.DeleteValue(curAssembly.GetName().Name);
                }
            } catch { }
        }
    }
}
