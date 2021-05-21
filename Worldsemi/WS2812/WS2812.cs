using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Worldsemi.WS2812 {
    public class WS2812Controller {

        private readonly GpioPin gpioPin;
        private readonly int numLeds;        
        private readonly byte[] data;        
        public WS2812Controller(GpioPin dataPin, int numLeds) {
            this.gpioPin = dataPin;
            this.numLeds = numLeds;
            this.data = new byte[this.numLeds * 3];

            this.gpioPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void SetColor(int index, byte red, byte green, byte blue) {
            this.data[index * 3 + 0] = green;
            this.data[index * 3 + 1] = red;
            this.data[index * 3 + 2] = blue;
        }

        public void Flush() {
            this.Reset();
            this.NativeFlush(this.gpioPin.PinNumber, this.data, 0, this.data.Length);
        }

        public void SetBuffer(byte[] buffer, int offset, int count) {
            offset &= ~0x00000001;

            if (count > this.numLeds * 2)
                throw new IndexOutOfRangeException();

            for (var i = offset; i < count; i += 2) {
                var led = i / 2;

                var color = (buffer[i + 1] << 8) | (buffer[i]);

                var red = (((color) & 0xF800) >> 11) << 3;
                var green = (((color) & 0x07E0) >> 5) << 2;
                var blue = ((color) & 0x001F) << 3;

                this.SetColor(led, (byte)red, (byte)green, (byte)blue);
            }
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
        private extern void NativeFlush(int dataPin, byte[] buffer8, int offset, int size);
    }
}
