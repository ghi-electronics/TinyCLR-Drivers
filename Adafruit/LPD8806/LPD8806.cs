using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Adafruit.LPD8806 {
    public sealed class LPD8806 {
        private const byte MASK = 0x80;

        private readonly int numLeds;
        private readonly byte[] data;
        private readonly SpiDevice spi;

        public LPD8806(SpiController spiBus, int numLeds, int speed = 2000000) {
            this.spi = spiBus.GetDevice(new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.None,
                Mode = SpiMode.Mode0,
                ClockFrequency = speed
            }
            );

            var latchBytes = ((numLeds + 63) / 64) * 3;
            this.numLeds = numLeds * 3;

            this.data = new byte[this.numLeds + latchBytes];

            for (var i = 0; i < this.numLeds; i++) {
                this.data[i] = MASK;
            }

            this.spi.Write(new byte[latchBytes]);

            this.DoUpdate();
        }

        private void DoUpdate() => this.spi.Write(this.data);

        public void SetColor(int index, byte red, byte green, byte blue) {
            var i = index * 3;

            this.data[i + 0] = (byte)(MASK | green);
            this.data[i + 1] = (byte)(MASK | red);
            this.data[i + 2] = (byte)(MASK | blue);

            this.DoUpdate();
        }
    }
}
