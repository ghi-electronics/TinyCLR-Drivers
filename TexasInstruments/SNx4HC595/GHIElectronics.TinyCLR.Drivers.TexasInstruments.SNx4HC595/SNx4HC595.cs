using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.SNx4HC595 {
    public class SNx4HC595 {
        private readonly GpioPin latch;
        private readonly SpiDevice spiBus;

        public SNx4HC595(string spiName, int chipSelect, int latchPin) {
            this.spiBus = SpiController.FromName(spiName).GetDevice(
                new SpiConnectionSettings() {
                    ChipSelectLine = chipSelect,
                    ChipSelectType = SpiChipSelectType.Gpio,
                    Mode = SpiMode.Mode0,
                    ClockFrequency = 25_000_000,
                    DataBitLength = 8
                });

            this.latch = GpioController.GetDefault().OpenPin(latchPin);
            this.latch.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void WriteBuffer(byte[] buffer) {
            this.latch.Write(GpioPinValue.Low);
            this.spiBus.Write(buffer);
            this.latch.Write(GpioPinValue.High);
        }
    }
}
