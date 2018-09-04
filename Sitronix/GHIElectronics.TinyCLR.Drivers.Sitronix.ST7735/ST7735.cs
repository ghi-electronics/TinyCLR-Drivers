using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Display;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Sitronix.ST7735 {
    public enum ST7735CommandId : byte {
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
    }

    public class ST7735 {
        private const byte MADCTL_MY = 0x80;
        private const byte MADCTL_MX = 0x40;
        private const byte MADCTL_MV = 0x20;
        private const byte MADCTL_BGR = 0x08;

        private readonly byte[] buffer1 = new byte[1];
        private readonly byte[] buffer4 = new byte[4];
        private readonly SpiDevice spi;
        private readonly GpioPin reset;
        private readonly GpioPin control;

        private int bpp;

        public DisplayDataFormat DataFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public static int MaxWidth { get; } = 160;
        public static int MaxHeight { get; } = 128;

        public static SpiConnectionSettings GetConnectionSettings(int chipSelectLine) => new SpiConnectionSettings(chipSelectLine) {
            Mode = SpiMode.Mode3,
            ClockFrequency = 12_000_000,
            DataBitLength = 8
        };

        public ST7735(SpiDevice spi, GpioPin control) : this(spi, control, null) {

        }

        public ST7735(SpiDevice spi, GpioPin control, GpioPin reset) {
            this.spi = spi;

            this.control = control;
            this.control.SetDriveMode(GpioPinDriveMode.Output);

            this.reset = reset;
            this.reset?.SetDriveMode(GpioPinDriveMode.Output);

            this.Reset();
            this.Initialize();
            this.SetDataFormat(DisplayDataFormat.Rgb565);
            this.SetDrawWindow(0, 0, ST7735.MaxWidth, ST7735.MaxHeight);
            this.Enable();
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
            this.SendCommand(ST7735CommandId.SWRESET);
            Thread.Sleep(120);

            this.SendCommand(ST7735CommandId.SLPOUT);
            Thread.Sleep(200);

            //Frame Rate
            this.SendCommand(0xB1);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);

            this.SendCommand(0xB2);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);

            this.SendCommand(0xB3);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);

            //Column inversion
            this.SendCommand(0xB4);
            this.SendData(0x07);

            //Power Sequence
            this.SendCommand(0xC0);
            this.SendData(0xA2); this.SendData(0x02); this.SendData(0x84);

            this.SendCommand(0xC1);
            this.SendData(0xC5);

            this.SendCommand(0xC2);
            this.SendData(0x0A); this.SendData(0x00);

            this.SendCommand(0xC3);
            this.SendData(0x8A); this.SendData(0x2A);

            this.SendCommand(0xC4);
            this.SendData(0x8A); this.SendData(0xEE);

            //VCOM
            this.SendCommand(0xC5);
            this.SendData(0x0E);

            //Gamma Sequence
            this.SendCommand(0xE0);
            this.SendData(0x0F); this.SendData(0x1A);
            this.SendData(0x0F); this.SendData(0x18);
            this.SendData(0x2F); this.SendData(0x28);
            this.SendData(0x20); this.SendData(0x22);
            this.SendData(0x1F); this.SendData(0x1B);
            this.SendData(0x23); this.SendData(0x37);
            this.SendData(0x00); this.SendData(0x07);
            this.SendData(0x02); this.SendData(0x10);

            this.SendCommand(0xE1);
            this.SendData(0x0F); this.SendData(0x1B);
            this.SendData(0x0F); this.SendData(0x17);
            this.SendData(0x33); this.SendData(0x2C);
            this.SendData(0x29); this.SendData(0x2E);
            this.SendData(0x30); this.SendData(0x30);
            this.SendData(0x39); this.SendData(0x3F);
            this.SendData(0x00); this.SendData(0x07);
            this.SendData(0x03); this.SendData(0x10);

            //Enable test command
            this.SendCommand(0xF0);
            this.SendData(0x01);

            //Disable ram power save mode
            this.SendCommand(0xF6);
            this.SendData(0x00);

            this.SendCommand(ST7735CommandId.MADCTL);
            this.SendData(MADCTL_MV | MADCTL_MY);
        }

        public void Enable() => this.SendCommand(ST7735CommandId.DISPON);
        public void Disable() => this.SendCommand(ST7735CommandId.DISPOFF);

        public void SendCommand(ST7735CommandId command) => this.SendCommand((byte)command);

        public void SendCommand(byte command) {
            this.buffer1[0] = command;
            this.control.Write(GpioPinValue.Low);
            this.spi.Write(this.buffer1);
        }

        public void SendData(byte data) {
            this.buffer1[0] = data;
            this.control.Write(GpioPinValue.High);
            this.spi.Write(this.buffer1);
        }

        public void SendData(byte[] data) {
            this.control.Write(GpioPinValue.High);
            this.spi.Write(data);
        }

        public void SetDataFormat(DisplayDataFormat colorFormat) {
            switch (colorFormat) {
                case DisplayDataFormat.Rgb444:
                    this.bpp = 12;
                    this.SendCommand(ST7735CommandId.COLMOD);
                    this.SendData(0x03);

                    break;

                case DisplayDataFormat.Rgb565:
                    this.bpp = 16;
                    this.SendCommand(ST7735CommandId.COLMOD);
                    this.SendData(0x05);

                    break;

                default:
                    throw new ArgumentException();
            }

            this.DataFormat = DisplayDataFormat.Rgb444;
        }

        public void SetDrawWindow(int x, int y, int width, int height) {
            this.Width = width;
            this.Height = height;

            this.buffer4[1] = (byte)x;
            this.buffer4[3] = (byte)(x + width - 1);
            this.SendCommand(ST7735CommandId.CASET);
            this.SendData(this.buffer4);

            this.buffer4[1] = (byte)y;
            this.buffer4[3] = (byte)(y + height - 1);
            this.SendCommand(ST7735CommandId.RASET);
            this.SendData(this.buffer4);
        }

        public void SendDrawCommand() {
            this.SendCommand(ST7735CommandId.RAMWR);
            this.control.Write(GpioPinValue.High);
        }

        public void DrawBuffer(byte[] buffer) => this.DrawBuffer(buffer, 0);

        public void DrawBuffer(byte[] buffer, int offset) {
            this.SendDrawCommand();

            this.spi.Write(buffer, offset, this.Height * this.Width * this.bpp / 8);
        }
    }
}
