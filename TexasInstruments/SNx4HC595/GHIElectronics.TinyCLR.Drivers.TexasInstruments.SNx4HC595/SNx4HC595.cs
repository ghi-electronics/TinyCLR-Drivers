using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.SNx4HC595 {
    public class SNx4HC595 {
        private readonly GpioPin latch;
        private readonly SpiDevice spiBus;

        public static SpiConnectionSettings GetSpiConnectionSettings(int chipSelect) => new SpiConnectionSettings {
            ChipSelectLine = chipSelect,
            ChipSelectType = SpiChipSelectType.Gpio,
            Mode = SpiMode.Mode0,
            ClockFrequency = 25_000_000,
            DataBitLength = 8
        };

        public SNx4HC595(SpiDevice spiBus, GpioPin latch) {
            this.spiBus = spiBus;

            this.latch = latch;
            this.latch.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void WriteBuffer(byte[] buffer) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            this.latch.Write(GpioPinValue.Low);
            this.spiBus.Write(buffer);
            this.latch.Write(GpioPinValue.High);
        }
    }
}
