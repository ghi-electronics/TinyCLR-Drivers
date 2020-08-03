using System;
using System.Drawing;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.ShijiLighting.APA102C {
    public class APA102CController : IDisposable {
        private readonly byte[] startFrame = { 0x00, 0x00, 0x00, 0x00 };
        private readonly byte[] stopFrame = { 0xFF, 0xFF, 0xFF, 0xFF };
        private readonly byte[] dataFrame;
        private readonly int ledCount;
        private readonly SpiDevice spi;

        public APA102CController(SpiController spiBus, int ledCount, int speed = 2_000_000) {
            this.dataFrame = new byte[ledCount * 4];
            this.ledCount = ledCount;
            this.spi = spiBus.GetDevice(new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.None,
                Mode = SpiMode.Mode0,
                ClockFrequency = speed
            }
            );

            for (var i = 0; i < this.dataFrame.Length; i += 4)
                this.dataFrame[i] = 0b1110_0000;
        }

        public void Dispose() => this.spi.Dispose();

        public void Set(int led, Color color) => this.Set(led, color, 0b0001_1111);

        public void Set(int led, Color color, int intensity) {
            if (led >= this.ledCount) throw new ArgumentOutOfRangeException(nameof(led));
            if (intensity > 0b0001_1111) throw new ArgumentOutOfRangeException(nameof(intensity));

            led *= 4;

            this.dataFrame[led] = (byte)(0b1110_0000 | intensity);
            this.dataFrame[led + 1] = color.B;
            this.dataFrame[led + 2] = color.G;
            this.dataFrame[led + 3] = color.R;
        }

        public void SetBuffer(byte[] buffer, int offset, int count) {
            if (count > this.dataFrame.Length)
                throw new IndexOutOfRangeException();

            Array.Copy(buffer, offset, this.dataFrame, 0, count);
        }

        public void Flush() {
            this.spi.Write(this.startFrame);
            this.spi.Write(this.dataFrame);
            this.spi.Write(this.stopFrame);
        }
    }
}
