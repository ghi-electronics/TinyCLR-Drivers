using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Infrared {
    public class NecIRDecoder {
        private enum Status {
            PreBurst,
            PostPreBurst,
            RepeatEnd,
            BurstBit,
            DataBit,

        }

        private Status status = Status.PreBurst;

        private const int BurstPreTime = 9000;
        private const int SpacePreTime = 4500;
        private const int BurstBitTime = 562;
        private const int ZeroBitTime = 562;
        private const int OneBitTime = 1688;
        private const int RepeatTime = 2250;
        private const int RepeatEndTime = 562;

        private long lastTick;

        private uint necMessage = 0;
        private int bitIndex = 0;

        public int ErrorCounter { get; set; }

        private readonly GpioPin receivePin;

        public delegate void RepeatEventHandler();
        public delegate void DataReceivedEventHandler(byte address, byte command);

        public event RepeatEventHandler OnRepeatEvent;
        public event DataReceivedEventHandler OnDataReceivedEvent;
        public NecIRDecoder(GpioPin receivePin) {

            this.receivePin = receivePin;

            this.lastTick = DateTime.Now.Ticks;

            this.receivePin.SetDriveMode(GpioPinDriveMode.Input);
            this.receivePin.ValueChangedEdge = GpioPinEdge.FallingEdge | GpioPinEdge.RisingEdge;
            this.receivePin.DebounceTimeout = TimeSpan.FromMilliseconds(0);
            this.receivePin.ValueChanged += this.Rx_ValueChanged;

            this.status = Status.PreBurst;
        }

        private bool InRange(int value, int expected) {
            var lowMargin = 0.8;
            var highMargin = 1.2;

            if ((value > (expected * lowMargin)) && (value < (expected * highMargin)))
                return true;

            return false;
        }
        private void Rx_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e) {

            var bitTime = (int)((e.Timestamp.Ticks - this.lastTick) / 10);
            this.lastTick = e.Timestamp.Ticks;

            switch (this.status) {
                case Status.PreBurst:
                    if (this.InRange(bitTime, BurstPreTime)) {
                        this.status = Status.PostPreBurst;
                        this.necMessage = 0;
                        this.bitIndex = 0;
                    }
                    break;
                case Status.PostPreBurst:
                    if (this.InRange(bitTime, SpacePreTime))
                        this.status = Status.BurstBit;
                    else if (this.InRange(bitTime, RepeatTime)) {
                        this.status = Status.RepeatEnd;
                    }
                    else {
                        this.ErrorCounter++;
                        this.status = Status.PreBurst;//error, go back!
                    }
                    break;
                case Status.RepeatEnd:
                    if (this.InRange(bitTime, RepeatEndTime)) {
                        OnRepeatEvent?.Invoke();

                    }
                    else {
                        this.ErrorCounter++;
                        this.status = Status.PreBurst;//error, go back!
                    }
                    break;
                case Status.BurstBit:
                    if (this.InRange(bitTime, BurstBitTime))
                        this.status = Status.DataBit;
                    else {
                        this.ErrorCounter++;
                        this.status = Status.PreBurst;//error, go back!
                    }
                    break;
                case Status.DataBit:
                    this.status = Status.BurstBit;
                    if (this.InRange(bitTime, ZeroBitTime)) {
                        // we have a zero
                        this.bitIndex++;
                    }
                    else if (this.InRange(bitTime, OneBitTime)) {
                        //we have a one
                        this.necMessage |= (uint)((1 << this.bitIndex));
                        this.bitIndex++;
                    }
                    else {
                        this.ErrorCounter++;
                        this.status = Status.PreBurst;//error, go back!
                    }
                    if (this.bitIndex == 32) {
                        var b0 = (byte)(this.necMessage >> 0);
                        var b1 = (byte)~(this.necMessage >> 8);
                        var b2 = (byte)(this.necMessage >> 16);
                        var b3 = (byte)~(this.necMessage >> 24);
                        if ((b0 == b1) && (b2 == b3)) {
                            OnDataReceivedEvent?.Invoke(b0, b2);
                        }
                        else {
                            this.ErrorCounter++;
                        }

                        this.status = Status.PreBurst;// we are good to restart

                    }
                    break;

                default:
                    this.status = Status.PreBurst;
                    break;

            }
        }
    }
}
