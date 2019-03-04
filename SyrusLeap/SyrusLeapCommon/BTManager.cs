using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;

namespace SyrusLeapCommon
{

    // The packet format
    // This class gets serialized and sent over the BT connection
    public struct SyrusPacket {
        public byte id, n;
        public byte[] data;
    }

    public delegate void PacketReceivedHandler(SyrusPacket pak);
    public delegate void ConnectedHandler();
    public delegate void DisconnectedHandler();

    public abstract class BTManager {
        protected StreamSocket socket;
        protected DataWriter writer;
        protected DataReader reader;
        protected BluetoothDevice bluetoothDevice;
        protected bool isConnected = false;

        public event PacketReceivedHandler PacketReceived;
        public event ConnectedHandler Connected;
        public event DisconnectedHandler Disconnected;

        public abstract void Initialize();

        public async void SendPacket(SyrusPacket pak) {
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
            Connected();
        }

        protected void OnDisconnected() {
            Disconnected();
        }

        protected void OnPacketReceived(SyrusPacket pak) {
            PacketReceived(pak);
        }
    }
}
