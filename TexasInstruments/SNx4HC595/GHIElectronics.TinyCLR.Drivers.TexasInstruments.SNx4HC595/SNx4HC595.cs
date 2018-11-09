using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.SNx4HC595 {
    public class SNx4HC595 {
        private readonly GpioPin latch;
        private readonly SpiDevice spiBus;

        public SNx4HC595(SpiDevice spiBus, int chipSelect, int latchPin) {
            this.spiBus = spiBus;
            //this.spiBus = SpiController.FromName(spiName).GetDevice(
            //    new SpiConnectionSettings() {
            //        ChipSelectLine = chipSelect,
            //        ChipSelectType = SpiChipSelectType.Gpio,
            //        Mode = SpiMode.Mode0,
            //        ClockFrequency = 25_000_000,
            //        DataBitLength = 8
            //    });

            this.latch = GpioController.GetDefault().OpenPin(latchPin);
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
