using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Signals;

namespace GHIElectronics.TinyCLR.Drivers.Worldsemi.WS2812 {
    public class WS2812Controller {
        private readonly DigitalSignal digitalSignalPin;
        private readonly SignalGenerator signalGeneratorPin;

        private readonly int numLeds;

        private readonly uint[] bufferTimming;
        private readonly TimeSpan[] timeSpanTimming;

        private readonly bool digitalSignalMode;

        public uint TimingHigh { get; set; }
        public uint TimingLow { get; set; }

        const int BYTE_PER_LED = 48; // 24 bit RGB  = 48 element

        public WS2812Controller(DigitalSignal dataPin, int numLeds) {
            this.digitalSignalPin = dataPin;
            this.digitalSignalMode = true;
            this.TimingHigh = 17;
            this.TimingLow = 8;
            this.numLeds = numLeds;
            this.bufferTimming = new uint[1 + this.numLeds * BYTE_PER_LED ];

            // reset
            // digitalSignal Reset command, 1000 * 2 * 50 (multiplier) = 100us            
            this.bufferTimming[0] = 1000 * 2;

            for (var i = 0; i < numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);

            this.Flush();
        }

        public WS2812Controller(SignalGenerator dataPin, int numLeds) {
            this.signalGeneratorPin = dataPin;
            this.TimingHigh = 5;
            this.TimingLow = 1;
            this.numLeds = numLeds;
            this.timeSpanTimming = new TimeSpan[1 + this.numLeds * BYTE_PER_LED];

            this.signalGeneratorPin.DisableInterrupts = true;
            this.signalGeneratorPin.IdleValue = GpioPinValue.Low;

            // reset            
            // signalgenerator Reset command, 100 * 10 () = 100us
            this.timeSpanTimming[this.timeSpanTimming.Length - 1] = TimeSpan.FromTicks(100 * 10);

            for (var i = 0; i < numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);

            this.Flush();
        }

        public void SetColor(int index, int red, int green, int blue) {
            var idx = 0;

            if (this.digitalSignalMode) {
                for (var i = 7; i >= 0; i--) {
                    if ((green & (1 << i)) > 0) {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 0 + 0 + idx] = this.TimingHigh;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 0 + 1 + idx] = this.TimingLow;
                    }
                    else {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 0 + 0 + idx] = this.TimingLow;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 0 + 1 + idx] = this.TimingHigh; ;
                    }

                    if ((red & (1 << i)) > 0) {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 16 + 0 + idx] = this.TimingHigh; ;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 16 + 1 + idx] = this.TimingLow;
                    }
                    else {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 16 + 0 + idx] = this.TimingLow;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 16 + 1 + idx] = this.TimingHigh; ;
                    }

                    if ((blue & (1 << i)) > 0) {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 32 + 0 + idx] = this.TimingHigh; ;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 32 + 1 + idx] = this.TimingLow;
                    }
                    else {
                        this.bufferTimming[1 + index * BYTE_PER_LED + 32 + 0 + idx] = this.TimingLow;
                        this.bufferTimming[1 + index * BYTE_PER_LED + 32 + 1 + idx] = this.TimingHigh; ;
                    }

                    idx += 2;

                }
            }
            else {
                for (var i = 7; i >= 0; i--) {
                    if ((green & (1 << i)) > 0) {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.TimingHigh);
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.TimingLow);
                    }
                    else {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 0 + 0 + idx] = TimeSpan.FromTicks(this.TimingLow);
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 0 + 1 + idx] = TimeSpan.FromTicks(this.TimingHigh); ;
                    }

                    if ((red & (1 << i)) > 0) {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.TimingHigh); ;
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.TimingLow);
                    }
                    else {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 16 + 0 + idx] = TimeSpan.FromTicks(this.TimingLow);
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 16 + 1 + idx] = TimeSpan.FromTicks(this.TimingHigh); ;
                    }

                    if ((blue & (1 << i)) > 0) {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.TimingHigh); ;
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.TimingLow);
                    }
                    else {
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 32 + 0 + idx] = TimeSpan.FromTicks(this.TimingLow);
                        this.timeSpanTimming[0 + index * BYTE_PER_LED + 32 + 1 + idx] = TimeSpan.FromTicks(this.TimingHigh); ;
                    }

                    idx += 2;
                }
            }
        }
        
        public void Flush() {
            // From 0....47 is first Led.
            // From 48....95 is second Led
            // ...
            // Last element is 100us (must be at least 50us) reset which latches the colors
            if (this.digitalSignalPin != null) {

                while (this.digitalSignalPin.CanGenerate == false) {
                    Thread.Sleep(1);
                }

                this.digitalSignalPin.Generate(this.bufferTimming, 0, (uint)this.bufferTimming.Length, 50);
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

        public void Clear() {
            for (var i = 0; i < this.numLeds; i++)
                this.SetColor(i, 0x00, 0x00, 0x00);
        }
    }
}
