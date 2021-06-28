using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Encoder {
    public class EncoderController : IDisposable {
        readonly GpioPin pinA;
        readonly GpioPin pinB;
        private int counter;
        private bool run = false;

        private TimeSpan timeout = TimeSpan.FromMilliseconds(100);

        public delegate void CounterChangedEvent(int counter);
        public event CounterChangedEvent OnCounterChangedEvent;

        public EncoderController(GpioPin pinA, GpioPin pinB) {
            this.pinA = pinA;
            this.pinB = pinB;

            this.pinA.SetDriveMode(GpioPinDriveMode.InputPullUp);
            this.pinB.SetDriveMode(GpioPinDriveMode.InputPullUp);

            this.run = true;

            new Thread(this.Process).Start();
        }
        private void Process() {
            while (this.run) {

                var valueA = this.pinA.Read() == GpioPinValue.High ? true : false;
                var valueB = this.pinB.Read() == GpioPinValue.High ? true : false;

                var expired = DateTime.Now + this.timeout;
                if (valueB == false) {

                    while (this.pinB.Read() == GpioPinValue.Low) {
                        if (DateTime.Now >= expired)
                            goto ignored;
                    }

                    if (this.pinA.Read() == GpioPinValue.Low) {
                        this.counter++;

                        OnCounterChangedEvent?.Invoke(this.counter);
                    }

                    continue;

                }

                if (valueA == false) {
                    while (this.pinA.Read() == GpioPinValue.Low) {
                        if (DateTime.Now >= expired)
                            goto ignored;
                    }

                    if (this.pinB.Read() == GpioPinValue.Low) {
                        this.counter--;

                        OnCounterChangedEvent?.Invoke(this.counter);
                    }
                }

            ignored:
                Thread.Sleep(20);
            }
        }

        public void Dispose() => this.run = false;

        public int Counter => this.counter;

    }
}
