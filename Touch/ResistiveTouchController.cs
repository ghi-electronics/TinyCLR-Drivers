using GHIElectronics.TinyCLR.Devices.Adc;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Touch.ResistiveTouch {
    public class ResistiveTouchController {
        private readonly int yu;
        private readonly int xl;
        private readonly int yd;
        private readonly int xr;

        private readonly int yuAnalogChannel;
        private readonly int xlAnalogChannel;

        private readonly AdcController adcController;
        private readonly GpioController gpioController;

        private readonly int screenWidth;
        private readonly int screenHeight;
        private readonly uint maxResolution;

        public int MinX { get; set; } = 10;
        public int MinY { get; set; } = 10;

        public int X => this.ReadX();
        public int Y => this.ReadY();

        public ResistiveTouchController(int screenWidth, int screenHeight, int yu, int xl, int yd, int xr, string adcControllerId, int yuAnalogChannel, int xlAnalogChannel) {
            this.yu = yu;
            this.xl = xl;
            this.yd = yd;
            this.xr = xr;

            this.yuAnalogChannel = yuAnalogChannel;
            this.xlAnalogChannel = xlAnalogChannel;

            this.adcController = AdcController.FromName(adcControllerId);
            this.gpioController = GpioController.GetDefault();

            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;

            for (var i = 0; i < this.adcController.ResolutionInBits; i++) {
                this.maxResolution |= (uint)(1 << i);
            }
        }

        private int ReadX() {
            var yp = this.gpioController.OpenPin(this.yu);
            var ym = this.gpioController.OpenPin(this.yd);
            var xp = this.gpioController.OpenPin(this.xr);
            var xm = this.gpioController.OpenPin(this.xl);

            yp.SetDriveMode(GpioPinDriveMode.InputPullDown);
            ym.SetDriveMode(GpioPinDriveMode.InputPullDown);

            xp.SetDriveMode(GpioPinDriveMode.Output);
            xm.SetDriveMode(GpioPinDriveMode.Output);

            xp.Write(GpioPinValue.High);
            xm.Write(GpioPinValue.Low);

            yp.Dispose();

            var analog = this.adcController.OpenChannel(this.yuAnalogChannel);

            var value = (analog.ReadValue());

            analog.Dispose();

            yp.Dispose();
            ym.Dispose();
            xp.Dispose();
            xm.Dispose();

            value = (int)(value * this.screenWidth / this.maxResolution);

            return value > this.MinX ? value : -1;
        }

        private int ReadY() {
            var yp = this.gpioController.OpenPin(this.yu);
            var ym = this.gpioController.OpenPin(this.yd);
            var xp = this.gpioController.OpenPin(this.xr);
            var xm = this.gpioController.OpenPin(this.xl);

            xp.SetDriveMode(GpioPinDriveMode.InputPullDown);
            xm.SetDriveMode(GpioPinDriveMode.InputPullDown);

            yp.SetDriveMode(GpioPinDriveMode.Output);
            ym.SetDriveMode(GpioPinDriveMode.Output);

            yp.Write(GpioPinValue.Low);
            ym.Write(GpioPinValue.High);

            xm.Dispose();

            var analog = this.adcController.OpenChannel(this.xlAnalogChannel);

            var value = (analog.ReadValue());

            analog.Dispose();
            yp.Dispose();
            ym.Dispose();
            xp.Dispose();
            xm.Dispose();

            value = (int)(value * this.screenHeight / this.maxResolution);

            return value > this.MinY ? value : -1;
        }
    }
}
