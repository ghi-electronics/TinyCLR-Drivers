using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.CD4017B {
    public class CD4017B {
        private readonly GpioPin clock;
        private readonly GpioPin reset;
        public int CurrentCount { get; private set; }

        public CD4017B(GpioPin clock, GpioPin reset) {
            this.clock.SetDriveMode(GpioPinDriveMode.Output);
            this.reset.SetDriveMode(GpioPinDriveMode.Output);
            this.ResetCountToZero();
        }

        public void ResetCountToZero() {
            this.reset.Write(GpioPinValue.High);
            this.reset.Write(GpioPinValue.Low);
            this.CurrentCount = 0;
        }

        public void IncreaseCount() {
            this.clock.Write(GpioPinValue.High);
            this.clock.Write(GpioPinValue.Low);
            this.CurrentCount++;
        }
    }
}
