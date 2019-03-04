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

    public abstract class BTManager {
        protected StreamSocket socket;
        protected DataWriter writer;
        protected DataReader reader;
        protected BluetoothDevice bluetoothDevice;

        public event PacketReceivedHandler PacketReceived;
        public event ConnectedHandler OnConnected;

        public abstract void Initialize();

        public async void SendPacket(SyrusPacket pak) {
            writer.WriteByte(Constants.StartCode);

            SendByte(pak.id);
            SendByte(pak.n);

            foreach (byte b in pak.data) {
                SendByte(b);
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
    }
}
