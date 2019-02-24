using SyrusLeapCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyrusLeapServer {
    class ServerBTManager : BTManager {

        private RfcommServiceProvider rfcommProvider;
        private StreamSocketListener socketListener;

        public override async void Initialize() {
            try {
                rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid));
            } // Catch exception HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE).
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF) {
                // The Bluetooth radio may be off.
                // TODO: Error managing
                System.Diagnostics.Debug.WriteLine("Error 1");
                return;
            }

            // Create a listener for this service and start listening
            socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += OnConnectionReceived;
            var rfcomm = rfcommProvider.ServiceId.AsString();

            await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(rfcommProvider);

            try {
                rfcommProvider.StartAdvertising(socketListener, true);
            } catch (Exception e) {
                // If you aren't able to get a reference to an RfcommServiceProvider, tell the user why.  Usually throws an exception if user changed their privacy settings to prevent Sync w/ Devices.  
                // TODO: Error managing
                System.Diagnostics.Debug.WriteLine("Error 2");
                return;
            }
        }

        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider) {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(Constants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)Constants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(Constants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(Constants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        private async void OnConnectionReceived(
            StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args) {
            System.Diagnostics.Debug.WriteLine("Connected to client");
            // Don't need the listener anymore
            socketListener.Dispose();
            socketListener = null;

            try {
                socket = args.Socket;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Error 3");
                Disconnect();
                return;
            }

            // Note - this is the supported way to get a Bluetooth device from a given socket
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(socket.Information.RemoteHostName);

            writer = new DataWriter(socket.OutputStream);
            var reader = new DataReader(socket.InputStream);
            bool remoteDisconnection = false;

            System.Diagnostics.Debug.WriteLine("Connected to client");

            // Infinite read buffer loop
            while (true) {
                try {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint)) {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength) {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);

                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3) {
                    System.Diagnostics.Debug.WriteLine("Client Disconnected");
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection) {
                Disconnect();
                System.Diagnostics.Debug.WriteLine("Client Disconnected");
            }
        }

        private void Disconnect() {
            if (rfcommProvider != null) {
                rfcommProvider.StopAdvertising();
                rfcommProvider = null;
            }

            if (socketListener != null) {
                socketListener.Dispose();
                socketListener = null;
            }

            if (writer != null) {
                writer.DetachStream();
                writer = null;
            }

            if (socket != null) {
                socket.Dispose();
                socket = null;
            }
        }

    }
}
