using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.SNx4HC595 {
    public class SNx4HC595 : IDisposable {
        private readonly GpioPin latch;
        private readonly SpiDevice spiBus;
        private byte[] invalidateBuffer;

        public static SpiConnectionSettings GetSpiConnectionSettings() => new SpiConnectionSettings {
            ChipSelectType = SpiChipSelectType.None,
            Mode = SpiMode.Mode0,
            ClockFrequency = 12_000_000,
            DataBitLength = 8
        };

        public SNx4HC595(SpiDevice spiBus, GpioPin latch) {
            this.spiBus = spiBus;

            this.latch = latch;
            this.latch.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void Dispose() {
            this.latch.Dispose();
            this.spiBus.Dispose();
        }

        public void WriteBuffer(byte[] buffer) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (this.invalidateBuffer == null || this.invalidateBuffer.Length != buffer.Length) {
                this.invalidateBuffer = new byte[buffer.Length];

                Array.Clear(this.invalidateBuffer, 0, this.invalidateBuffer.Length);
            }

            this.latch.Write(GpioPinValue.Low);
            this.spiBus.Write(buffer);
            this.latch.Write(GpioPinValue.High);
        }

        public void Invalidate() {
            if (this.invalidateBuffer != null) {
                this.latch.Write(GpioPinValue.Low);
                this.spiBus.Write(this.invalidateBuffer);
                this.latch.Write(GpioPinValue.High);
            }
        }
    }
}
