using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Neopixel.WS2812 {
    public class WS2812Controller {
        private readonly SignalGenerator signalPin;
        private readonly int numLeds;
        private readonly TimeSpan[] bufferColor;

        public int HighTick { get; set; } = 5;
        public int LowTick { get; set; } = 1;

        const int BYTE_PER_LED = 48; // 24 bit RGB  = 48 element

        public WS2812Controller(GpioPin dataPin, int numLeds) {
            this.signalPin = new SignalGenerator(dataPin) {
                DisableInterrupts = true,
                IdleValue = GpioPinValue.High
            };

            this.numLeds = numLeds;

            this.bufferColor = new TimeSpan[this.numLeds * BYTE_PER_LED + 1];
            this.bufferColor[0] = TimeSpan.FromTicks(100 * 10); // Reset command, 100us

            for (var i = 0; i < numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);

            this.Flush();
        }

        public void SetColor(int ledIndex, int red, int green, int blue) {
            var idx = 0;

            for (var i = 7; i >=0; i--) {
                if ((green & (1<<i)) > 0) {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.HighTick);
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.LowTick);
                }
                else {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.LowTick);
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.HighTick); ;
                }

                if ((red & (1 << i)) > 0) {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.HighTick); ;
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.LowTick);
                }
                else {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.LowTick);
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.HighTick); ;
                }

                if ((blue & (1 << i)) > 0) {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.HighTick); ;
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.LowTick);
                }
                else {
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.LowTick);
                    this.bufferColor[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.HighTick); ;
                }

                idx += 2;

            }
        }

        public void Flush() {
            if (this.signalPin != null) {
                // First element is reset
                // From 1....48 is first Led.
                // From 49....97 is second Led
                // and so on.
                this.signalPin.Write(this.bufferColor);
            }
        }

        public void SetBuffer(byte[] buffer, int offset, int count) {
            if (count > this.bufferColor.Length)
                throw new IndexOutOfRangeException();

            Array.Copy(buffer, offset, this.bufferColor, 0, count);
        }
    }
}
