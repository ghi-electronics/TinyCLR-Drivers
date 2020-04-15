using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Adc;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.MixedSignalIntegrated.MSGEQ7 {
    public sealed class Msgeq7 {
        private AdcChannel adcSignal;

        public int[] Data { get; private set; }

        private readonly GpioPin strobePin;
        private readonly GpioPin resetPin;

        private const int MAX_BAND = 7;

        public Msgeq7(AdcChannel adcSignal, GpioPin strobePin, GpioPin resetPin) {
            this.adcSignal = adcSignal;

            this.strobePin = strobePin;
            this.resetPin = resetPin;

            this.strobePin.SetDriveMode(GpioPinDriveMode.Output);
            this.resetPin.SetDriveMode(GpioPinDriveMode.Output);

            this.Data = new int[MAX_BAND];

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
                this.Data[i] = this.adcSignal.ReadValue();

                var t1 = DateTime.Now.Ticks;

                this.strobePin.Write(GpioPinValue.High);
                while ((DateTime.Now.Ticks - t1) / 10 < 50) ; // wait 50us

                this.strobePin.Write(GpioPinValue.Low);
                while ((DateTime.Now.Ticks - t1) / 10 < 100) ;  // wait 50us
            }
        }
    }
}
