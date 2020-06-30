using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.SNx4HC595 {
    public class SNx4HC595 : IDisposable {
        private readonly GpioPin latch;
        private readonly GpioPin reset;
        private readonly SpiDevice spiBus;

        public SNx4HC595(SpiController spiController, GpioPin latch, GpioPin reset, int clock = 12_000_000) {
            var settings = new SpiConnectionSettings {
                ChipSelectType = SpiChipSelectType.None,
                Mode = SpiMode.Mode0,
                ClockFrequency = clock
            };

            this.spiBus = spiController.GetDevice(settings);

            this.latch = latch;
            this.latch.SetDriveMode(GpioPinDriveMode.Output);

            this.reset = reset;
            this.reset.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void Dispose() {
            this.latch.Dispose();
            this.spiBus.Dispose();
            this.reset.Dispose();
        }

        public void WriteBuffer(byte[] buffer) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            this.spiBus.Write(buffer);
            this.Latch();
        }

        public void Clear() {
            this.Reset();
            this.Latch();
        }

        private void Latch() {
            this.latch.Write(GpioPinValue.High);
            this.latch.Write(GpioPinValue.Low);
        }

        private void Reset() {
            this.reset.Write(GpioPinValue.Low);
            this.reset.Write(GpioPinValue.High);
        }
    }
}
