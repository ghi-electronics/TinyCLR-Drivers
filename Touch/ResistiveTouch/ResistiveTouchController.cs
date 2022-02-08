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
   
    public class ResistiveTouchSetting {
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public int YU { get; set; }
        public int XL { get; set; }
        public int YD { get; set; }
        public int XR { get; set; }
        public string YUAdcControllerId { get; set; }
        public string XLAdcControllerId { get; set; }
        public int YUAnalogChannel { get; set; }
        public int XLAnalogChannel { get; set; }
    }

    public class ResistiveTouchController {
        private readonly int yu;
        private readonly int xl;
        private readonly int yd;
        private readonly int xr;

        private readonly int yuAnalogChannel;
        private readonly int xlAnalogChannel;

        private readonly AdcController yuAdcController;
        private readonly AdcController xlAdcController;
        private readonly GpioController gpioController;

        private readonly int screenWidth;
        private readonly int screenHeight;

        public int MinX { get; set; } = 10;
        public int MinY { get; set; } = 10;

        public int X => this.ReadX();
        public int Y => this.ReadY();

        public Scale ScaleX { get; set; }
        public Scale ScaleY { get; set; }

        public ResistiveTouchController(ResistiveTouchSetting setting) {
            this.yu = setting.YU;
            this.xl = setting.XL;
            this.yd = setting.YD;
            this.xr = setting.XR;

            this.yuAnalogChannel = setting.YUAnalogChannel;
            this.xlAnalogChannel = setting.XLAnalogChannel;

            this.yuAdcController = AdcController.FromName(setting.YUAdcControllerId);
            this.xlAdcController = AdcController.FromName(setting.XLAdcControllerId);

            this.gpioController = GpioController.GetDefault();

            this.screenWidth = setting.ScreenWidth;
            this.screenHeight = setting.ScreenHeight;

            this.ScaleX = new Scale(0, this.screenWidth);
            this.ScaleY = new Scale(0, this.screenHeight);

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

            var analog = this.yuAdcController.OpenChannel(this.yuAnalogChannel);

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

            var analog = this.xlAdcController.OpenChannel(this.xlAnalogChannel);

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
