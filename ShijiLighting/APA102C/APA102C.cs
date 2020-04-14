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

        public static SpiConnectionSettings GetConnectionSettings() => new SpiConnectionSettings {
            Mode = SpiMode.Mode0,
            ClockFrequency = 1_200_000,
            ChipSelectType = SpiChipSelectType.None
        };

        public APA102CController(SpiDevice spi, int ledCount) {
            this.dataFrame = new byte[ledCount * 4];
            this.ledCount = ledCount;
            this.spi = spi;

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

        public void Flush() {
            this.spi.Write(this.startFrame);
            this.spi.Write(this.dataFrame);
            this.spi.Write(this.stopFrame);
        }
    }
}
