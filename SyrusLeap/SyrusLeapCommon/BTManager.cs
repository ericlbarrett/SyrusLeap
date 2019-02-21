using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SyrusLeapCommon
{
    public struct SyrusPacket {
        public byte id, n;
        public byte[] data;
    }

    public delegate void PacketReceivedHandler(SyrusPacket pak);

    public abstract class BTManager {
        protected StreamSocket socket;
        protected DataWriter writer;

        public event PacketReceivedHandler PacketReceived;

        public abstract void Initialize();

        public bool SendPacket(SyrusPacket pak) {
            writer.WriteByte(Constants.StartCode);

            SendByte(pak.id);
            SendByte(pak.n);

            foreach (byte b in pak.data) {
                SendByte(b);
            }

            writer.WriteByte(Constants.EndCode);

            return true;
        }

        protected void SendByte(byte b) {
            if (b == Constants.StartCode || b == Constants.EndCode || b == Constants.EscCode) {
                writer.WriteByte(Constants.EscCode);
            }
            writer.WriteByte(b);
        }
    }
}
