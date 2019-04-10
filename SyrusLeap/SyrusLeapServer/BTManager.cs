using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;

namespace SyrusLeapServer {

    // The packet format
    // This class gets serialized and sent over the BT connection
    public struct SyrusPacket {
        public byte id, n; // Might need to allow for bigger packets eventually
        public byte[] data;
    }

    public delegate void PacketReceivedHandler(SyrusPacket pak);
    public delegate void ConnectedHandler();
    public delegate void DisconnectedHandler();

    public abstract class BTManager {
        protected StreamSocket socket;
        protected DataWriter writer;
        protected DataReader reader;
        protected BluetoothDevice bluetoothDevice; // Maybe we don't need this?? Move to ClientBTManager only.
        protected bool isConnected = false;

        public event PacketReceivedHandler PacketReceived;
        public event ConnectedHandler Connected;
        public event DisconnectedHandler Disconnected;

        public abstract void Initialize();

        public async void SendPacket(SyrusPacket pak) {
            if (!isConnected) {
                throw new System.InvalidOperationException("Controller not connected");
            }

            writer.WriteByte(Constants.StartCode);

            SendByte(pak.id);
            SendByte(pak.n);

            if (pak.data != null) {
                foreach (byte b in pak.data) {
                    SendByte(b);
                }
            }

            writer.WriteByte(Constants.EndCode);
            await writer.StoreAsync();
        }

        protected void SendByte(byte b) {
            if (b == Constants.StartCode || b == Constants.EndCode || b == Constants.EscCode) {
                writer.WriteByte(Constants.EscCode);
            }
            writer.WriteByte(b);
        }

        protected void OnConnected() {
            if (Connected != null) Connected();
        }

        protected void OnDisconnected() {
            if (Disconnected != null) Disconnected();
        }

        protected void OnPacketReceived(SyrusPacket pak) {
            if (PacketReceived != null) PacketReceived(pak);
        }
    }
}
