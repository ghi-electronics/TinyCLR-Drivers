using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Worldsemi.WS2812 {
    public class WS2812Controller {
        public enum DataFormat {
            rgb888 = 0,
            rgb565 = 1
        };

        private readonly GpioPin gpioPin;
        private readonly uint numLeds;
        private readonly byte[] data;
        private DataFormat bpp;
        private long resetPulse = 10 * 50 * 2;

        public byte[] Buffer => this.data;
        public TimeSpan ResetPulse {
            get => TimeSpan.FromTicks(this.resetPulse);

            set => this.resetPulse = value.Ticks;
        }

        public WS2812Controller(GpioPin dataPin, uint numLeds, DataFormat bpp) {
            this.gpioPin = dataPin;
            this.numLeds = numLeds;
            this.bpp = bpp;

            if (bpp == DataFormat.rgb888) {
                this.data = new byte[this.numLeds * 3];
            }
            else {
                this.data = new byte[this.numLeds * 2];
            }

            this.gpioPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void SetColor(int index, byte red, byte green, byte blue) {
            if (this.bpp == DataFormat.rgb888) {
                this.data[index * 3 + 0] = green;
                this.data[index * 3 + 1] = red;
                this.data[index * 3 + 2] = blue;
            }
            else {
                red >>= 3;
                green >>= 2;
                blue >>= 3;

                var color16 = (ushort)((red << 11) | (green << 5) | (blue << 0));

                this.data[index * 2 + 1] = (byte)(color16 >> 8);
                this.data[index * 2 + 0] = (byte)(color16 >> 0);
            }
        }

        public void Flush() => this.Flush(true);

        public void Flush(bool reset) {
            this.NativeFlush(this.gpioPin.PinNumber, this.data, 0, this.data.Length, this.bpp == DataFormat.rgb888);

            if (reset)
                this.Reset();
        }


        public void Clear() {
            for (var i = 0; i < this.numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);
        }

        public void Reset() {
            this.gpioPin.Write(GpioPinValue.Low);

            var expired = DateTime.Now.Ticks + this.resetPulse;

            while (DateTime.Now.Ticks < expired) ;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeFlush(int dataPin, byte[] buffer8, int offset, int size, bool bpp24);
    }
}
