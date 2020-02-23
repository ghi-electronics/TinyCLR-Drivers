using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Adc;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.MixedSignalIntegrated.MSGEQ7 {
    public sealed class MSGEQ7 {
        private AdcChannel channelLeft;
        private AdcChannel channelRight;

        public int[] BandsLeft { get; private set; }
        public int[] BandsRight { get; private set; }

        private readonly GpioPin strobePin;
        private readonly GpioPin resetPin;

        private const int MAX_BAND = 7;

        public AdcController AdcController { get; set; } = AdcController.GetDefault();
        public GpioController GpioController { get; set; } = GpioController.GetDefault();
        public MSGEQ7(int adcChannelLeft, int adcChannelRight, int strobePin, int resetPin) {
            this.channelLeft = this.AdcController.OpenChannel(adcChannelLeft);
            this.channelRight = this.AdcController.OpenChannel(adcChannelRight);

            this.strobePin = this.GpioController.OpenPin(strobePin);
            this.resetPin = this.GpioController.OpenPin(resetPin);

            this.strobePin.SetDriveMode(GpioPinDriveMode.Output);
            this.resetPin.SetDriveMode(GpioPinDriveMode.Output);

            this.BandsLeft = new int[MAX_BAND];
            this.BandsRight = new int[MAX_BAND];

            this.Reset();
        }

        private void Reset() {
            this.resetPin.Write(GpioPinValue.High);
            this.strobePin.Write(GpioPinValue.High);
            Thread.Sleep(1);

            this.strobePin.Write(GpioPinValue.Low);
            Thread.Sleep(1);

            this.resetPin.Write(GpioPinValue.High);
            Thread.Sleep(1);

            this.strobePin.Write(GpioPinValue.High);
            Thread.Sleep(1);

            this.strobePin.Write(GpioPinValue.Low);
            Thread.Sleep(1);

            this.resetPin.Write(GpioPinValue.Low);
            Thread.Sleep(1);
        }

        public void UpdateBands() {
            for (var i = 0; i < MAX_BAND; i++) {
                this.BandsLeft[i] = this.channelLeft.ReadValue();
                this.BandsRight[i] = this.channelRight.ReadValue();

                var t1 = DateTime.Now.Ticks;

                this.strobePin.Write(GpioPinValue.High);
                while ((DateTime.Now.Ticks - t1) / 10 < 50) ; // wait 50us

                this.strobePin.Write(GpioPinValue.Low);
                while ((DateTime.Now.Ticks - t1) / 10 < 100) ;  // wait 50us
            }
        }        
    }
}
