using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Sitronix.ST7789 {
    public enum ST7789CommandId : byte {
        //System
        NOP = 0x00,
        SWRESET = 0x01,
        RDDID = 0x04,
        RDDST = 0x09,
        RDDPM = 0x0A,
        RDDMADCTL = 0x0B,
        RDDCOLMOD = 0x0C,
        RDDIM = 0x0D,
        RDDSM = 0x0E,
        SLPIN = 0x10,
        SLPOUT = 0x11,
        PTLON = 0x12,
        NORON = 0x13,
        INVOFF = 0x20,
        INVON = 0x21,
        GAMSET = 0x26,
        DISPOFF = 0x28,
        DISPON = 0x29,
        CASET = 0x2A,
        RASET = 0x2B,
        RAMWR = 0x2C,
        RAMRD = 0x2E,
        PTLAR = 0x30,
        TEOFF = 0x34,
        TEON = 0x35,
        MADCTL = 0x36,
        IDMOFF = 0x38,
        IDMON = 0x39,
        COLMOD = 0x3A,
        RDID1 = 0xDA,
        RDID2 = 0xDB,
        RDID3 = 0xDC,

        //Panel
        FRMCTR1 = 0xB1,
        FRMCTR2 = 0xB2,
        FRMCTR3 = 0xB3,
        INVCTR = 0xB4,
        DISSET5 = 0xB6,
        PWCTR1 = 0xC0,
        PWCTR2 = 0xC1,
        PWCTR3 = 0xC2,
        PWCTR4 = 0xC3,
        PWCTR5 = 0xC4,
        VMCTR1 = 0xC5,
        VMOFCTR = 0xC7,
        WRID2 = 0xD1,
        WRID3 = 0xD2,
        NVCTR1 = 0xD9,
        NVCTR2 = 0xDE,
        NVCTR3 = 0xDF,
        GAMCTRP1 = 0xE0,
        GAMCTRN1 = 0xE1,
    }

    public enum DataFormat {
        Rgb565 = 0,
        Rgb444 = 1
    }

    public class ST7789Controller {
        private readonly byte[] buffer1 = new byte[1];
        private readonly byte[] buffer4 = new byte[4];
        private readonly SpiDevice spi;
        private readonly GpioPin control;
        private readonly GpioPin reset;

        private int bpp;
        private bool rowColumnSwapped;

        public DataFormat DataFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int MaxWidth => 240;
        public int MaxHeight => 240;

        public static SpiConnectionSettings GetConnectionSettings(SpiChipSelectType chipSelectType, GpioPin chipSelectLine) => new SpiConnectionSettings {
            Mode = SpiMode.Mode3,
            ClockFrequency = 12_000_000,
            ChipSelectType = chipSelectType,
            ChipSelectLine = chipSelectLine
        };

        public ST7789Controller(SpiDevice spi, GpioPin control) : this(spi, control, null) {

        }

        public ST7789Controller(SpiDevice spi, GpioPin control, GpioPin reset) {
            this.spi = spi;

            this.control = control;
            this.control.SetDriveMode(GpioPinDriveMode.Output);

            this.reset = reset;
            this.reset?.SetDriveMode(GpioPinDriveMode.Output);

            this.Reset();
            this.Initialize();
            this.SetDataFormat(DataFormat.Rgb565);
            this.SetDataAccessControl(false, false, false, false);
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
            this.SendCommand((ST7789CommandId)0x36);
            this.SendData(0x70);

            this.SendCommand((ST7789CommandId)0x3A);
            this.SendData(0x05);

            this.SendCommand((ST7789CommandId)0xB2);
            this.SendData(0x0C);
            this.SendData(0x0C);
            this.SendData(0x00);
            this.SendData(0x33);
            this.SendData(0x33);

            this.SendCommand((ST7789CommandId)0xB7);
            this.SendData(0x35);

            this.SendCommand((ST7789CommandId)0xBB);
            this.SendData(0x19);

            this.SendCommand((ST7789CommandId)0xC0);
            this.SendData(0x2C);

            this.SendCommand((ST7789CommandId)0xC2);
            this.SendData(0x01);

            this.SendCommand((ST7789CommandId)0xC3);
            this.SendData(0x12);

            this.SendCommand((ST7789CommandId)0xC4);
            this.SendData(0x20);

            this.SendCommand((ST7789CommandId)0xC6);
            this.SendData(0x0F);

            this.SendCommand((ST7789CommandId)0xD0);
            this.SendData(0xA4);
            this.SendData(0xA1);

            this.SendCommand((ST7789CommandId)0xE0);
            this.SendData(0xD0);
            this.SendData(0x04);
            this.SendData(0x0D);
            this.SendData(0x11);
            this.SendData(0x13);
            this.SendData(0x2B);
            this.SendData(0x3F);
            this.SendData(0x54);
            this.SendData(0x4C);
            this.SendData(0x18);
            this.SendData(0x0D);
            this.SendData(0x0B);
            this.SendData(0x1F);
            this.SendData(0x23);

            this.SendCommand((ST7789CommandId)0xE1);
            this.SendData(0xD0);
            this.SendData(0x04);
            this.SendData(0x0C);
            this.SendData(0x11);
            this.SendData(0x13);
            this.SendData(0x2C);
            this.SendData(0x3F);
            this.SendData(0x44);
            this.SendData(0x51);
            this.SendData(0x2F);
            this.SendData(0x1F);
            this.SendData(0x1F);
            this.SendData(0x20);
            this.SendData(0x23);

            this.SendCommand((ST7789CommandId)0x21);

            this.SendCommand((ST7789CommandId)0x11);
            Thread.Sleep(120);

            this.SendCommand((ST7789CommandId)0x29);
        }

        public void Dispose() {
            this.spi.Dispose();
            this.control.Dispose();
            this.reset?.Dispose();
        }

        public void Enable() => this.SendCommand(ST7789CommandId.DISPON);
        public void Disable() => this.SendCommand(ST7789CommandId.DISPOFF);

        private void SendCommand(ST7789CommandId command) {
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
            var val = default(byte);

            if (useBgrPanel) val |= 0b0000_1000;
            if (swapRowColumn) val |= 0b0010_0000;
            if (invertColumn) val |= 0b0100_0000;
            if (invertRow) val |= 0b1000_0000;

            this.SendCommand(ST7789CommandId.MADCTL);
            this.SendData(val);

            this.rowColumnSwapped = swapRowColumn;
        }

        public void SetDataFormat(DataFormat dataFormat) {
            switch (dataFormat) {
                case DataFormat.Rgb444:
                    this.bpp = 12;
                    this.SendCommand(ST7789CommandId.COLMOD);
                    this.SendData(0x03);

                    break;

                case DataFormat.Rgb565:
                    this.bpp = 16;
                    this.SendCommand(ST7789CommandId.COLMOD);
                    this.SendData(0x05);

                    break;

                default:
                    throw new NotSupportedException();
            }

            this.DataFormat = dataFormat;
        }

        public void SetDrawWindow(int x, int y, int width, int height) {
            this.Width = width;
            this.Height = height;

            this.buffer4[1] = (byte)x;
            this.buffer4[3] = (byte)(x + width - 1);
            this.SendCommand(ST7789CommandId.CASET);
            this.SendData(this.buffer4);

            this.buffer4[1] = (byte)y;
            this.buffer4[3] = (byte)(y + height - 1);
            this.SendCommand(ST7789CommandId.RASET);
            this.SendData(this.buffer4);
        }

        private void SendDrawCommand() {
            this.SendCommand(ST7789CommandId.RAMWR);
            this.control.Write(GpioPinValue.High);
        }

        public void DrawBuffer(byte[] buffer) {
            this.SendDrawCommand();

            if (this.bpp == 16)
                BitConverter.SwapEndianness(buffer, 2);

            this.spi.Write(buffer);

            if (this.bpp == 16)
                BitConverter.SwapEndianness(buffer, 2);
        }

        public void DrawBuffer(byte[] buffer, int x, int y, int width, int height) {
            this.SetDrawWindow(x, y, width, height);

            this.DrawBuffer(buffer, x, y, width, height, this.MaxWidth, 1, 1);
        }

        public void DrawBuffer(byte[] buffer, int x, int y, int width, int height, int originalWidth, int columnMultiplier, int rowMultiplier) {
            if (this.bpp != 16)
                throw new NotSupportedException(); // Multiplier does suppport 16bbp only

            this.SendDrawCommand();

            BitConverter.SwapEndianness(buffer, 2);

            this.spi.Write(buffer, x, y, width, height, originalWidth, columnMultiplier, rowMultiplier);

            BitConverter.SwapEndianness(buffer, 2);
        }
    }
}
