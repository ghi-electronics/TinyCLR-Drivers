using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Neopixel.WS2812 {
    public class WS2812Controller {
        private readonly DigitalSignal digitalSignalPin;
        private readonly SignalGenerator signalGeneratorPin;

        private readonly int numLeds;

        private readonly uint[] bufferTimming;
        private readonly TimeSpan[] timeSpanTimming;

        private readonly bool digitalSignalMode;

        public uint BitOne { get; set; }
        public uint BitZero { get; set; }
        public uint DigitalSignalMultiplier { get; set; } = 400;//ns

        const int BYTE_PER_LED = 48; // 24 bit RGB  = 48 element

        public WS2812Controller(DigitalSignal dataPin, int numLeds) {
            this.digitalSignalPin = dataPin;
            this.digitalSignalMode = true;
            this.BitOne = 2;
            this.BitZero = 1;
            this.numLeds = numLeds;
            this.bufferTimming = new uint[this.numLeds * BYTE_PER_LED + 1];

            // reset
            // digitalSignal Reset command, 100 * 3 * 400 (multiplier) = 120us            
            this.bufferTimming[0] = 100 * 3;

            for (var i = 0; i < numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);

            this.Flush();
        }

        public WS2812Controller(SignalGenerator dataPin, int numLeds) {
            this.signalGeneratorPin = dataPin;
            this.BitOne = 5;
            this.BitZero = 1;
            this.numLeds = numLeds;
            this.timeSpanTimming = new TimeSpan[this.numLeds * BYTE_PER_LED + 1];

            this.signalGeneratorPin.DisableInterrupts = true;
            this.signalGeneratorPin.IdleValue = GpioPinValue.High;

            // reset            
            // signalgenerator Reset command, 100 * 10 () = 100us
            this.timeSpanTimming[0] = TimeSpan.FromTicks(100 * 10);

            for (var i = 0; i < numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);

            this.Flush();
        }

        public void SetColor(int ledIndex, int red, int green, int blue) {
            var idx = 0;

            if (this.digitalSignalMode) {
                for (var i = 7; i >= 0; i--) {
                    if ((green & (1 << i)) > 0) {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = this.BitOne;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = this.BitZero;
                    }
                    else {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = this.BitZero;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = this.BitOne; ;
                    }

                    if ((red & (1 << i)) > 0) {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = this.BitOne; ;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = this.BitZero;
                    }
                    else {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = this.BitZero;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = this.BitOne; ;
                    }

                    if ((blue & (1 << i)) > 0) {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = this.BitOne; ;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = this.BitZero;
                    }
                    else {
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = this.BitZero;
                        this.bufferTimming[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = this.BitOne; ;
                    }

                    idx += 2;

                }
            }
            else {
                for (var i = 7; i >= 0; i--) {
                    if ((green & (1 << i)) > 0) {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.BitOne);
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.BitZero);
                    }
                    else {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.BitZero);
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.BitOne); ;
                    }

                    if ((red & (1 << i)) > 0) {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.BitOne); ;
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.BitZero);
                    }
                    else {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.BitZero);
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.BitOne); ;
                    }

                    if ((blue & (1 << i)) > 0) {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.BitOne); ;
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.BitZero);
                    }
                    else {
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.BitZero);
                        this.timeSpanTimming[1 + ledIndex * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.BitOne); ;
                    }

                    idx += 2;
                }
            }
        }

        public void Flush() {
            // First element is reset
            // From 1....48 is first Led.
            // From 49....97 is second Led
            // and so on.
            if (this.digitalSignalPin != null) {

                while (this.digitalSignalPin.CanWrite == false) {
                    Thread.Sleep(1);
                }

                this.digitalSignalPin.Write(this.bufferTimming, 0, (uint)this.bufferTimming.Length, 400, GpioPinValue.Low);
            }
            else {
                this.signalGeneratorPin.Write(this.timeSpanTimming);
            }
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

                this.SetColor(led, red, green, blue);
            }
        }
    }
}
