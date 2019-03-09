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
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyrusLeapClient {
    class ClientBTManager : BTManager {

        private DeviceWatcher deviceWatcher = null; // DeviceWatcher to find the server
        private RfcommDeviceService service = null; // The service that communication happens on

        public override async void Initialize() {
            System.Diagnostics.Debug.WriteLine("Initializing ClientBTManager");
            StartWatcher();
            System.Diagnostics.Debug.WriteLine("Client Succesfully Initialized.");
        }

        private async void StartWatcher() {
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            // TODO: These would preferably be split into dedicated functions + we might not need all of these.
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) => {
                System.Diagnostics.Debug.WriteLine("Found Device: " + deviceInfo.Name + " " + deviceInfo.Id);
                // TODO: Hardcode intel stick name
                Connect(deviceInfo);
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

            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(devInfo.Id).CurrentStatus;
            if (accessStatus != DeviceAccessStatus.Allowed) {
                System.Diagnostics.Debug.WriteLine("Access State: " + accessStatus);
                System.Diagnostics.Debug.WriteLine("This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices");
                return;
            }

            // TODO: Maybe automatic pairing?

            try {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(devInfo.Id);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("Error: Couldn't get BluetoothDevice");
                return;
            }

            if (bluetoothDevice == null) {
                System.Diagnostics.Debug.WriteLine("Bluetooth Device returned null. Access Status = " + accessStatus.ToString());
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid), BluetoothCacheMode.Uncached); // Maybe change to cached???

            if (rfcommServices.Services.Count > 0) {
                service = rfcommServices.Services[0];
            } else {
                System.Diagnostics.Debug.WriteLine("Error: Could not discover the chat service on the remote device");
                System.Diagnostics.Debug.WriteLine("Paired: " + devInfo.Pairing.IsPaired);
                System.Diagnostics.Debug.WriteLine("Connection Status: " + bluetoothDevice.ConnectionStatus);
                return;
            }
            
            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await service.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId)) {
                System.Diagnostics.Debug.WriteLine(
                    "The service is not advertising the Service Name attribute (attribute id=0x100).");
                return;
            }
            var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != Constants.SdpServiceNameAttributeType) {
                System.Diagnostics.Debug.WriteLine(
                    "The Chat service is using an unexpected format for the Service Name attribute. ");
                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

            StopWatcher();

            lock (this) {
                socket = new StreamSocket();
            } try {
                await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName);
                
                writer = new DataWriter(socket.OutputStream);
                reader = new DataReader(socket.InputStream);

                System.Diagnostics.Debug.WriteLine("Connected to Server");
                isConnected = true;
                OnConnected();

                mainLoop();
            } catch (Exception ex) when ((uint)ex.HResult == 0x80070490) {// ERROR_ELEMENT_NOT_FOUND
                System.Diagnostics.Debug.WriteLine("Please verify that you are running the BluetoothRfcommChat server.");
            } catch (Exception ex) when ((uint)ex.HResult == 0x80072740) { // WSAEADDRINUSE
                System.Diagnostics.Debug.WriteLine("Please verify that there is no other RFCOMM connection to the same device.");
            }
        }

        private async void mainLoop() {
            bool escaped = false;
            int index = -1;
            SyrusPacket packet = new SyrusPacket();

            while (true) {
                try {
                    uint size = await reader.LoadAsync(sizeof(byte));
                    if (size < sizeof(byte)) {
                        Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
                        return;
                    }

                    byte b = reader.ReadByte();

                    if (!escaped) {
                        if (b == Constants.StartCode) {
                            index = 0;
                            packet = new SyrusPacket();
                        } else if (b == Constants.EndCode) {
                            // Check if the data matches what the packet said
                            if (index >= 3 && packet.n == index - 3) {
                                OnPacketReceived(packet);
                            }
                            index = -1;
                        } else if (b == Constants.EscCode) {
                            escaped = true;
                            index--;
                            
                        } else if (index - 3 >= packet.n) {
                            // Error with the packet
                            index = -1;
                        } else if (index == 1) {
                            packet.id = b;
                        } else if (index == 2) {
                            packet.n = b;
                            packet.data = new byte[b];
                        } else if (index > 2) {
                            packet.data[index - 3] = b;
                        }
                    } else {
                        if (index - 3 >= packet.n) {
                            // Error with the packet
                            index = -1;
                        } else if (index == 1) {
                            packet.id = b;
                        } else if (index == 2) {
                            packet.n = b;
                            packet.data = new byte[b];
                        } else if (index > 2) {
                            packet.data[index - 3] = b;
                        }
                        escaped = false;
                    }
                    
                    if (index >= 0) index++;

                } catch (Exception ex) {
                    lock (this) {
                        if (socket == null) {
                            // Do not print anything here -  the user closed the socket.
                            if ((uint)ex.HResult == 0x80072745)
                                System.Diagnostics.Debug.WriteLine("Disconnect triggered by remote device");
                            else if ((uint)ex.HResult == 0x800703E3)
                                System.Diagnostics.Debug.WriteLine("The I/O operation has been aborted because of either a thread exit or an application request.");
                        } else {
                            Disconnect("Read stream failed with error: " + ex.Message);
                        }
                    }
                }

            }
        }

        private void StopWatcher(){
            if (deviceWatcher != null) {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                     deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted) {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }

        private void Disconnect(string disconnectReason) {
            isConnected = false;

            if (writer != null) {
                writer.DetachStream();
                writer = null;
            }

            if (reader != null) {
                reader.DetachStream();
                reader = null;
            }

            if (service != null) {
                service.Dispose();
                service = null;
            }
            lock (this) {
                if (socket != null) {
                    socket.Dispose();
                    socket = null;
                }
            }

            if (bluetoothDevice != null) {
                //bluetoothDevice.Close();
                bluetoothDevice = null;
            }

            System.Diagnostics.Debug.WriteLine("Disconnected from Server:" + disconnectReason);
            OnDisconnected();
        }        
    }
}
