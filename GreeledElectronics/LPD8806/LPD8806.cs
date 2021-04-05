using System;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.GreeledElectronics.LPD8806 {
    public sealed class LPD8806Controller {
        private const byte MASK = 0x80;

        private readonly int numLeds;
        private readonly byte[] data;
        private readonly SpiDevice spi;
        private readonly int latchBytes;
        
        public LPD8806Controller(SpiController spiBus, int numLeds, int speed = 2000000) {
            this.spi = spiBus.GetDevice(new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.None,
                Mode = SpiMode.Mode0,
                ClockFrequency = speed
            }
            );

            this.latchBytes = ((numLeds + 63) / 64) * 3;
            this.numLeds = numLeds * 3;

            this.data = new byte[this.numLeds + this.latchBytes];

            Array.Clear(this.data, 0, this.data.Length);

            for (var i = 0; i < this.numLeds; i++) {
                this.data[i] = MASK;
            }            

            this.Flush();
        }

        public void Flush() => this.spi.Write(this.data);

        public void SetColor(int index, byte red, byte green, byte blue) {
            var i = index * 3;

            this.data[i + 0] = (byte)(MASK | green);
            this.data[i + 1] = (byte)(MASK | red);
            this.data[i + 2] = (byte)(MASK | blue);
        }

        public void SetBuffer(byte[] buffer, int offset, int count) {
            if (count > this.data.Length - this.latchBytes)
                throw new IndexOutOfRangeException();

            Array.Copy(buffer, offset, this.data, 0, count);
        }
    }
}
