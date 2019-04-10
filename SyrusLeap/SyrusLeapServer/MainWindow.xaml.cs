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

namespace SyrusLeapServer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        ServerBTManager sbm;

        public MainWindow() {
            InitializeComponent();

            sbm = new ServerBTManager();
            sbm.Initialize();
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e) {
            SyrusPacket pak = new SyrusPacket();

            pak.id = 24;

            pak.data = System.Text.Encoding.ASCII.GetBytes(TextBox.Text);
            pak.n = (byte)pak.data.Length;
            //pak.data = new byte[3];
            //pak.data[0] = 24;
            //pak.data[1] = 100;
            //pak.data[2] = 17;

            sbm.SendPacket(pak);
        }
    }
}
