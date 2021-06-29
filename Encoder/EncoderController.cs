using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Encoder {
    public class EncoderController {
        readonly GpioPin pinA;
        readonly GpioPin pinB;

        public delegate void CounterChangedEvent(int counter);
        public event CounterChangedEvent OnCounterChangedEvent;

        private long timeAStart, timeAEnd;
        private long timeB;
        private int counter;

        public EncoderController(GpioPin pinA, GpioPin pinB) {
            this.pinA = pinA;
            this.pinB = pinB;

            this.pinA.SetDriveMode(GpioPinDriveMode.InputPullUp);
            this.pinB.SetDriveMode(GpioPinDriveMode.InputPullUp);

            this.pinA.DebounceTimeout = TimeSpan.FromMilliseconds(1);
            this.pinB.DebounceTimeout = TimeSpan.FromMilliseconds(1);

            this.pinA.ValueChanged += this.PinA_ValueChanged;
            this.pinB.ValueChanged += this.PinB_ValueChanged;

            this.pinA.ValueChangedEdge = GpioPinEdge.FallingEdge | GpioPinEdge.RisingEdge;
            this.pinB.ValueChangedEdge = GpioPinEdge.FallingEdge;
        }


        private void PinB_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e) {
            var valueB = this.pinB.Read() == GpioPinValue.High ? true : false;

            if (valueB == false)
                this.timeB = DateTime.Now.Ticks;
            else
                this.timeB = 0;
        }

        private void PinA_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e) {

            var valueA = this.pinA.Read() == GpioPinValue.High ? true : false;

            if (this.timeAStart == 0) {
                if (valueA == true)
                    return;

                this.timeAStart = DateTime.Now.Ticks;
            }
            else if (valueA == false) {
                this.timeAStart = 0;
                this.timeAEnd = 0;

                return;
            }
            else {
                this.timeAEnd = DateTime.Now.Ticks;

                if (this.timeB < this.timeAEnd && this.timeB > this.timeAStart) {
                    this.counter--;

                    OnCounterChangedEvent?.Invoke(this.counter);
                }
                else if (this.timeB < this.timeAStart && this.timeB != 0) {
                    this.counter++;

                    OnCounterChangedEvent?.Invoke(this.counter);
                }


                this.timeAStart = 0;
                this.timeAEnd = 0;
            }
        }
        public int Counter => this.counter;

    }
}
