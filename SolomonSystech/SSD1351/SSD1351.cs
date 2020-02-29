using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Display;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.SolomonSystech.SSD1351 {
    public enum SSD1351CommandId : byte {
        NOP = 0x00,
        SETCOLUMN = 0x15,
        SETROW = 0x75,
        WRITERAM = 0x5C,
        READRAM = 0x5D,
        SETREMAP = 0xA0,
        STARTLINE = 0xA1,
        DISPLAYOFFSET = 0xA2,
        DISPLAYALLOFF = 0xA4,
        DISPLAYALLON = 0xA5,
        NORMALDISPLAY = 0xA6,
        INVERTDISPLAY = 0xA7,
        FUNCTIONSELECT = 0xAB,
        DISPLAYOFF = 0xAE,
        DISPLAYON = 0xAF,
        PRECHARGE = 0xB1,
        DISPLAYENHANCE = 0xB2,
        CLOCKDIV = 0xB3,
        SETVSL = 0xB4,
        SETGPIO = 0xB5,
        PRECHARGE2 = 0xB6,
        SETGRAY = 0xB8,
        USELUT = 0xB9,
        PRECHARGELEVEL = 0xBB,
        VCOMH = 0xBE,
        CONTRASTABC = 0xC1,
        CONTRASTMASTER = 0xC7,
        MUXRATIO = 0xCA,
        COMMANDLOCK = 0xFD,
        HORIZSCROLL = 0x96,
        STOPSCROLL = 0x9E,
        STARTSCROLL = 0x9F
    }

    public class SSD1351Controller {
        private readonly byte[] buffer1 = new byte[1];
        private readonly byte[] buffer2 = new byte[2];

        private readonly SpiDevice spi;
        private readonly GpioPin control;
        private readonly GpioPin reset;

        private bool rowColumnSwapped;

        public DisplayDataFormat DataFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int MaxWidth => this.rowColumnSwapped ? 96 : 128;
        public int MaxHeight => this.rowColumnSwapped ? 128 : 96;

        public static SpiConnectionSettings GetConnectionSettings(SpiChipSelectType chipSelectType, int chipSelectLine) => new SpiConnectionSettings {
            Mode = SpiMode.Mode3,
            ClockFrequency = 8_000_000,
            DataBitLength = 8,
            ChipSelectType = chipSelectType,
            ChipSelectLine = chipSelectLine
        };

        public SSD1351Controller(SpiDevice spi, GpioPin control) : this(spi, control, null) {

        }

        public SSD1351Controller(SpiDevice spi, GpioPin control, GpioPin reset) {
            this.spi = spi;

            this.control = control;
            this.control.SetDriveMode(GpioPinDriveMode.Output);

            this.reset = reset;
            this.reset?.SetDriveMode(GpioPinDriveMode.Output);

            this.Reset();
            this.Initialize();
            this.SetDataFormat(DisplayDataFormat.Rgb565);
            this.SetDataAccessControl(false, true, true, false);
            this.SetDrawWindow(0, 0, this.MaxWidth, this.MaxHeight);
        }

        private void Reset() {
            if (this.reset == null)
                return;

            this.reset.Write(GpioPinValue.Low);
            Thread.Sleep(50);

            this.reset.Write(GpioPinValue.High);
            Thread.Sleep(200);
        }

        private void Initialize() {
            this.SendCommand(SSD1351CommandId.COMMANDLOCK);
            this.SendData(0x12);
            this.SendCommand(SSD1351CommandId.COMMANDLOCK);
            this.SendData(0xB1);

            this.SendCommand(SSD1351CommandId.DISPLAYOFF);
            this.SendCommand(SSD1351CommandId.MUXRATIO);
            this.SendData(127);

            this.SendCommand(SSD1351CommandId.SETREMAP);
            this.SendData(0x74);

            this.SendCommand(SSD1351CommandId.SETCOLUMN);
            this.SendData(0x00);
            this.SendData(0x7F);
            this.SendCommand(SSD1351CommandId.SETROW);
            this.SendData(0x00);
            this.SendData(0x7F);

            this.SendCommand(SSD1351CommandId.STARTLINE);
            this.SendData(0);

            this.SendCommand(SSD1351CommandId.DISPLAYOFFSET);
            this.SendData(0x0);

            this.SendCommand(SSD1351CommandId.SETGPIO);
            this.SendData(0x00);

            this.SendCommand(SSD1351CommandId.FUNCTIONSELECT);
            this.SendData(0x01);

            this.SendCommand(SSD1351CommandId.NORMALDISPLAY);

            this.SendCommand(SSD1351CommandId.CONTRASTABC);
            this.SendData(0xC8);
            this.SendData(0x80);
            this.SendData(0xC8);

            this.SendCommand(SSD1351CommandId.CONTRASTMASTER);
            this.SendData(0x0F);

            this.SendCommand(SSD1351CommandId.SETVSL);
            this.SendData(0xA0);
            this.SendData(0xB5);
            this.SendData(0x55);

            this.SendCommand(SSD1351CommandId.PRECHARGE2);
            this.SendData(0x01);

            this.SendCommand(SSD1351CommandId.DISPLAYON);
        }

        public void Dispose() {
            this.spi.Dispose();
            this.control.Dispose();
            this.reset?.Dispose();
        }

        private void SendCommand(SSD1351CommandId command) {
            this.buffer1[0] = (byte)command;
            this.control.Write(GpioPinValue.Low);
            this.spi.Write(this.buffer1);
        }

        private void SendData(byte data) {
            this.buffer1[0] = data;
            this.control.Write(GpioPinValue.High);
            this.spi.Write(this.buffer1);
        }

        private void SendData(byte[] data) {
            this.control.Write(GpioPinValue.High);
            this.spi.Write(data);
        }

        public void SetDataAccessControl(bool swapRowColumn, bool invertRow, bool invertColumn, bool useBgrPanel) {
            byte val = 0b01100100;

            if (useBgrPanel) val = 0b0110_0000;

            if (swapRowColumn) val |= 0b0000_0001;
            if (invertColumn) val |= 0b0000_0010;
            if (invertRow) val |= 0b0001_0000;

            this.SendCommand(SSD1351CommandId.SETREMAP);
            this.SendData(val);

            this.SendCommand(SSD1351CommandId.STARTLINE);
            this.SendData(invertRow ? (byte)this.Height : (byte)0);

            this.rowColumnSwapped = swapRowColumn;
        }

        public void SetDataFormat(DisplayDataFormat dataFormat) => this.DataFormat = dataFormat;

        private void SetDrawWindow(int x, int y, int width, int height) {
            this.Width = width;
            this.Height = height;

            var x2 = x + width - 1;
            var y2 = y + height - 1;

            if (this.rowColumnSwapped) {
                var tmp = x;
                x = y;
                y = tmp;

                tmp = x2;
                x2 = y2;
                y2 = tmp;
            }

            this.buffer2[0] = (byte)x;
            this.buffer2[1] = (byte)(x2);

            this.SendCommand(SSD1351CommandId.SETCOLUMN);
            this.SendData(this.buffer2);

            this.buffer2[0] = (byte)y;
            this.buffer2[1] = (byte)(y2);

            this.SendCommand(SSD1351CommandId.SETROW);
            this.SendData(this.buffer2);

            this.SendCommand(SSD1351CommandId.WRITERAM);
        }

        private void SendDrawCommand() => this.control.Write(GpioPinValue.High);

        public void DrawBuffer(byte[] buffer) {
            this.SendDrawCommand();
            this.spi.Write(buffer, 0, buffer.Length);
        }
    }
}
