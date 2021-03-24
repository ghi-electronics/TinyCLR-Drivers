using System;
using System.IO;
using System.Threading;
using GHIElectronics.TinyCLR.Cryptography;

namespace GHIElectronics.TinyCLR.Drivers.XModem {
    // Support 1K packet receive only
    public class XModem {
        const byte SOH = 0x01;
        const byte STX = 0x02;
        const byte EOT = 0x04;
        const byte ACK = 0x06;
        const byte NAK = 0x15;
        const byte CAN = 0x18;

        private Stream stream;
        private byte nextPacketNumber;
        private byte[] buffer;
        private Crc16 crc;
        public enum Variants {
            XModem1K
        };

        public Variants Variant { get; } = Variants.XModem1K;


        public XModem(Stream stream) {
            this.stream = stream;
            this.buffer = new byte[1024];
            this.nextPacketNumber = 1;
            this.crc = new Crc16();
        }

        public void Reset() {
            this.nextPacketNumber = 1;
            this.crc.Reset();
        }
                
        public byte[] ReceivePacket() {
            if (this.nextPacketNumber == 1) {
                while (this.stream.DataAvailable == false) {
                    this.stream.WriteByte((byte)'C');

                    Thread.Sleep(250);
                }
            }

            var header = (byte)this.stream.ReadByte();

            if (header == EOT) {
                // end of transfer
                this.AckPacket();

                return null;
            }

            var packetNumber = (byte)this.stream.ReadByte();
            var inversePacketNumber = (byte)this.stream.ReadByte();
            ushort receivedCRC;

            if (header != STX && header != SOH) {
                this.Abort();
                throw new Exception("Invalid header.");
            }

            if ((packetNumber | inversePacketNumber) != 0xFF) {
                this.Abort();
                throw new Exception("Invalid packetNumber.");
            }

            if (packetNumber != this.nextPacketNumber) {
                this.Abort();
                throw new Exception("Invalid packetNumber.");
            }

            this.stream.Read(this.buffer, 0, this.buffer.Length);

            receivedCRC = (ushort)((byte)this.stream.ReadByte() << 8);
            receivedCRC |= (ushort)((byte)this.stream.ReadByte() << 0);

            this.crc.Reset();

            if (receivedCRC != this.crc.ComputeHash(this.buffer, 0, this.buffer.Length)) {
                this.Abort();
                throw new Exception("Detected crc error.");
            }

            this.nextPacketNumber++;

            this.AckPacket();

            return this.buffer;
        }

        private void AckPacket() => this.stream.WriteByte(ACK);

        private void Abort() {
            this.stream.WriteByte(NAK);
            this.stream.WriteByte(CAN);
            this.stream.WriteByte(CAN);
        }
    }
}
