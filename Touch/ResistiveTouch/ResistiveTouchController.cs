using GHIElectronics.TinyCLR.Devices.Adc;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Touch.ResistiveTouch {
    public class Scale {
        public Scale(int min, int max) {
            this.Min = min;
            this.Max = max;
        }

        public int Min { get; set; }
        public int Max { get; set; }
    }

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

        public int MinX { get; set; } = 10;
        public int MinY { get; set; } = 10;

        public int X => this.ReadX();
        public int Y => this.ReadY();

        public Scale ScaleX { get; set; }
        public Scale ScaleY { get; set; }

        /// <summary>Resistive Touch constructor</summary>
        /// <param name="screenWidth">Screen size.</param>
        /// <param name="screenHeight">Screen size.</param>
        /// <param name="yu">The analog channel on the YU pin.</param>
        /// <param name="xl">The analog channel on the XL pin.</param>
        /// <param name="yd">The YD pin.</param>
        /// <param name="xr">The XR pin.</param>
        /// <param name="adcControllerId">Adc controller id.</param>
        /// <param name="yuAnalogChannel">yu analog channel id.</param>
        /// <param name="xlAnalogChannel">xl analog channel id.</param>
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

            this.ScaleX = new Scale(0, screenWidth);
            this.ScaleY = new Scale(0, screenHeight);

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

            var value = analog.ReadRatio();

            analog.Dispose();

            yp.Dispose();
            ym.Dispose();
            xp.Dispose();
            xm.Dispose();

            value *= this.screenWidth;

            return value < this.MinX ? -1 : Scale(value, this.ScaleX.Min, this.ScaleX.Max, 0, this.screenWidth);
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

            var value = analog.ReadRatio();

            analog.Dispose();
            yp.Dispose();
            ym.Dispose();
            xp.Dispose();
            xm.Dispose();

            value *= this.screenHeight;

            return value < this.MinX ? -1 : Scale(value, this.ScaleY.Min, this.ScaleY.Max, 0, this.screenHeight);
        }

        static int Scale(double value, int originalMin, int originalMax, int scaleMin, int scaleMax) {
            var scale = (double)(scaleMax - scaleMin) / (originalMax - originalMin);
            var ret = (int)(scaleMin + ((value - originalMin) * scale));

            return ret > scaleMax ? scaleMax : (ret < scaleMin ? scaleMin : ret);
        }
    }
}
