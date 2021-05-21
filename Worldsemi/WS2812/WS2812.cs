using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Worldsemi.WS2812 {
    public class WS2812Controller {

        private enum Bpp {
            rgb888 = 0,
            rgb565 = 1
        };

        private readonly GpioPin gpioPin;
        private readonly int numLeds;
        private readonly byte[] data;
        private Bpp bpp;        

        public WS2812Controller(GpioPin dataPin, int numLeds) : this(dataPin, numLeds, null) {

        }

        public WS2812Controller(GpioPin dataPin, int numLeds, byte[] data) {
            this.gpioPin = dataPin;
            this.numLeds = numLeds;

            if (data == null) {
                this.data = new byte[this.numLeds * 3];

                this.bpp = Bpp.rgb888;
            }
            else {
                this.data = data;

                if (data != null && data.Length == this.numLeds * 3) {
                    this.bpp = Bpp.rgb888;
                }
                else if (data != null && data.Length == this.numLeds * 2) {
                    this.bpp = Bpp.rgb565;
                }
                else
                    throw new ArgumentException("Support 24bpp or 16bpp array only.");
            }

            this.gpioPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void SetColor(int index, byte red, byte green, byte blue) {
            if (this.bpp == Bpp.rgb888) {
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

        public void Flush() {
            this.Reset();
            this.NativeFlush(this.gpioPin.PinNumber, this.data, 0, this.data.Length, this.bpp == Bpp.rgb888);
        }

        public void Flush(byte[] data, int offset, int count) {                        
            if ((count != this.numLeds * 3) && (count != this.numLeds * 2)) {
                throw new ArgumentException("Support 24bpp or 16bpp array only.");
            }

            var bpp24 = count == this.numLeds * 3;

            this.Reset();
            this.NativeFlush(this.gpioPin.PinNumber, data, offset, count, bpp24);
        }

        public void Clear() {
            for (var i = 0; i < this.numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);
        }

        private void Reset() {
            this.gpioPin.Write(GpioPinValue.Low);

            var expired = DateTime.Now.Ticks + 10 * 50 * 2;
            while (DateTime.Now.Ticks < expired) ;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeFlush(int dataPin, byte[] buffer8, int offset, int size, bool bpp24);
    }
}
