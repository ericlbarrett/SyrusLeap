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

namespace SyrusLeapClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ClientBTManager cbm;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            cbm = new ClientBTManager();
            cbm.PacketReceived += Recieved;
            cbm.Initialize();

        }

        private void SendBtn_Click(object sender, RoutedEventArgs e) {
            SyrusPacket pak = new SyrusPacket();

            pak.id = 23;
            pak.n = 0;

            cbm.SendPacket(pak);
        }

        private async void Recieved(SyrusPacket packet) {
            switch (packet.id) {
                case 24: {

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>  {
                        Text.Text = System.Text.ASCIIEncoding.ASCII.GetString(packet.data);
                    });
                    break;
                }

            }
        }
    }
}
