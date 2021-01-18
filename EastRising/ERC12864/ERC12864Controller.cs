using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.EastRising.ERC12864 {
    public class ERC12864Controller {
        private readonly byte[] vram;

        private readonly SpiDevice spi;
        private readonly GpioPin control;
        private readonly GpioPin reset;

        public int Width => 128;
        public int Height => 64;

        public static SpiConnectionSettings GetConnectionSettings(SpiChipSelectType chipSelectType, GpioPin chipSelectLine) => new SpiConnectionSettings {
            Mode = SpiMode.Mode0,
            ClockFrequency = 2_000_000,
            ChipSelectType = chipSelectType,
            ChipSelectLine = chipSelectLine
        };

        public ERC12864Controller(SpiDevice spi, GpioPin control) : this(spi, control, null) {

        }

        public ERC12864Controller(SpiDevice spi, GpioPin ctrl, GpioPin reset) {
            this.vram = new byte[this.Width * this.Height / 8];

            this.spi = spi;

            this.control = ctrl;
            this.reset = reset;

            this.control.SetDriveMode(GpioPinDriveMode.Output);
            this.reset.SetDriveMode(GpioPinDriveMode.Output);

            this.Reset();

            this.Initialize();
        }

        private void Reset() {
            this.reset?.Write(GpioPinValue.Low);
            Thread.Sleep(100);
            this.reset?.Write(GpioPinValue.High);
            Thread.Sleep(100);
        }

        private void Initialize() {
            this.SendCommand(0xa1);
            this.SendCommand(0xc8);
            this.SendCommand(0xa2);

            this.PowerControl(0x07);
            this.RegulorResistorSelect(0x05);
            this.SetContrastControlRegister(21);
            this.InitialDisplayLine(0x00);
            this.DisplayOn();
        }

        private void DisplayOn() => this.SendCommand(0xaf);

        private void PowerControl(byte volt) => this.SendCommand((byte)(0x28 | volt));

        private void RegulorResistorSelect(byte r) => this.SendCommand((byte)(0x20 | r));

        private void SetContrastControlRegister(byte mod) {
            this.SendCommand((byte)(0x81));
            this.SendCommand((byte)(mod));
        }

        private void InitialDisplayLine(byte line) {
            line |= 0x40;
            this.SendCommand(line);
        }

        private void SendCommand(byte command) {
            this.control.Write(GpioPinValue.Low);
            this.spi.Write(new byte[] { command }, 0, 1);
        }

        private void SendData(byte[] data, int offset, int count) {
            this.control.Write(GpioPinValue.High);
            this.spi.Write(data, offset, count);
        }

        private void SetPageAddress(int add) {
            add = 0xb0 | add;
            this.SendCommand((byte)add);
        }

        private void SetColumnAddress(int add) {
            this.SendCommand((byte)(0x10 | (add >> 4)));
            this.SendCommand((byte)((0x0f & add) | 0X04));
        }

        private void Flush() {
            var offset = 0;
            for (var i = 0; i < 8; i++) {
                this.SetPageAddress(i);
                this.SetColumnAddress(0);                

                this.SendData(this.vram, offset, this.Width);

                offset += this.Width;
            }
        }

        public void Enable() => this.SendCommand(0xaf);
        public void Disable() => this.SendCommand(0xae);

        public void Dispose() {
            this.spi.Dispose();
            this.control.Dispose();
            this.reset?.Dispose();
        }

        public void DrawBuffer(byte[] buffer) {
            Color.Convert(buffer, Color.RgbFormat.Rgb565, this.vram, Color.RgbFormat.Rgb1bit, this.Width);           

            this.Flush();
        }       
    }
}
