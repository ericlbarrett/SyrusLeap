using SyrusLeapCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace SyrusLeapClient {
    class ClientBTManager : BTManager {

        private DeviceWatcher deviceWatcher = null;
        private RfcommDeviceService chatService = null;
        private BluetoothDevice bluetoothDevice;

        public override void Initialize() {
            System.Diagnostics.Debug.WriteLine("Initializing ClientBTManager");


            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) => {
                System.Diagnostics.Debug.WriteLine("Found " + deviceInfo.Name + " " + deviceInfo.Id);
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) => {
                System.Diagnostics.Debug.WriteLine("DeviceWatcher updated.");
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) => {
                System.Diagnostics.Debug.WriteLine("Enumeration completed.");

            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) => {
                System.Diagnostics.Debug.WriteLine("Enumeration completed.");

            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) => {
                System.Diagnostics.Debug.WriteLine("DeviceWatcher stopped.");
            });

            deviceWatcher.Start();
        }

        private async void Connect(DeviceInformation devInfo) {

        }

        /*private async void ConnectButton_Click(object sender, RoutedEventArgs e) {
            // Make sure user has selected a device first
            if (resultsListView.SelectedItem != null) {
                rootPage.NotifyUser("Connecting to remote device. Please wait...", NotifyType.StatusMessage);
            } else {
                rootPage.NotifyUser("Please select an item to connect to", NotifyType.ErrorMessage);
                return;
            }

            RfcommChatDeviceDisplay deviceInfoDisp = resultsListView.SelectedItem as RfcommChatDeviceDisplay;

            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(deviceInfoDisp.Id).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser) {
                rootPage.NotifyUser("This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices", NotifyType.ErrorMessage);
                return;
            }
            // If not, try to get the Bluetooth device
            try {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
            } catch (Exception ex) {
                rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                ResetMainUI();
                return;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null) {
                rootPage.NotifyUser("Bluetooth Device returned null. Access Status = " + accessStatus.ToString(), NotifyType.ErrorMessage);
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0) {
                chatService = rfcommServices.Services[0];
            } else {
                rootPage.NotifyUser(
                   "Could not discover the chat service on the remote device",
                   NotifyType.StatusMessage);
                ResetMainUI();
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await chatService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId)) {
                rootPage.NotifyUser(
                    "The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                ResetMainUI();
                return;
            }
            var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != Constants.SdpServiceNameAttributeType) {
                rootPage.NotifyUser(
                    "The Chat service is using an unexpected format for the Service Name attribute. " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                ResetMainUI();
                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

            StopWatcher();

            lock (this) {
                chatSocket = new StreamSocket();
            }
            try {
                await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);

                SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);
                chatWriter = new DataWriter(chatSocket.OutputStream);

                DataReader chatReader = new DataReader(chatSocket.InputStream);
                ReceiveStringLoop(chatReader);
            } catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
              {
                rootPage.NotifyUser("Please verify that you are running the BluetoothRfcommChat server.", NotifyType.ErrorMessage);
                ResetMainUI();
            } catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
              {
                rootPage.NotifyUser("Please verify that there is no other RFCOMM connection to the same device.", NotifyType.ErrorMessage);
                ResetMainUI();
            }
        }

        */
    }
}
