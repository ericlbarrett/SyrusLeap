using SyrusLeapCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SyrusLeapServer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ServerBTManager sbm;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            sbm = new ServerBTManager();
            sbm.Initialize();
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e) {
            SyrusPacket pak = new SyrusPacket();

            pak.id = 24;
            pak.n = 3;
            pak.data = new byte[3];
            pak.data[0] = 24;
            pak.data[1] = 100;
            pak.data[2] = 17;

            sbm.SendPacket(pak);
        }
    }
}
