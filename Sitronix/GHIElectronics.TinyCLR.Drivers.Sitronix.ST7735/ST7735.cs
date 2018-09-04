using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Sitronix.ST7735 {
    public enum ColorFormat {
        Bgr12bit444 = 12,
        Bgr16bit565 = 16
    }

    public class ST7735 {
        private const byte ST7735_MADCTL = 0x36;
        private const byte MADCTL_MY = 0x80;
        private const byte MADCTL_MX = 0x40;
        private const byte MADCTL_MV = 0x20;
        private const byte MADCTL_BGR = 0x08;

        private readonly SpiDevice spi;
        private readonly GpioPin reset;
        private readonly GpioPin control;

        private readonly byte[] buffer1;
        private readonly byte[] buffer2;
        private readonly byte[] buffer4;

        private int drawWindowX;
        private int drawWindowY;
        private int drawWindowWidth;
        private int drawWindowHeight;

        private ColorFormat bitsPerPixel;

        public const int MaxWidth = 160;
        public const int MaxHeight = 128;

        public static SpiConnectionSettings GetConnectionSettings(int chipSelectLine) => new SpiConnectionSettings(chipSelectLine) { Mode = SpiMode.Mode3, ClockFrequency = 12_000_000, DataBitLength = 8 };

        public ST7735(SpiDevice spi, GpioPin control) : this(spi, control, null) {

        }

        public ST7735(SpiDevice spi, GpioPin control, GpioPin reset) {
            this.buffer1 = new byte[1];
            this.buffer2 = new byte[2];
            this.buffer4 = new byte[4];

            this.bitsPerPixel = ColorFormat.Bgr16bit565;

            this.spi = spi;

            this.control = control;
            this.control.SetDriveMode(GpioPinDriveMode.Output);

            this.reset = reset;
            this.reset?.SetDriveMode(GpioPinDriveMode.Output);

            this.Reset();
            this.Initialize();
            this.SetDrawWindow(0, 0, ST7735.MaxWidth, ST7735.MaxHeight);
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
            this.SendCommand(0x01); // Software Reset Command
            Thread.Sleep(120);

            this.SendCommand(0x11); //Sleep exit
            Thread.Sleep(200);

            // ST7735R Frame Rate
            this.SendCommand(0xB1);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);
            this.SendCommand(0xB2);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);
            this.SendCommand(0xB3);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);
            this.SendData(0x01); this.SendData(0x2C); this.SendData(0x2D);

            this.SendCommand(0xB4); // Column inversion
            this.SendData(0x07);

            // ST7735R Power Sequence
            this.SendCommand(0xC0);
            this.SendData(0xA2); this.SendData(0x02); this.SendData(0x84);
            this.SendCommand(0xC1); this.SendData(0xC5);
            this.SendCommand(0xC2);
            this.SendData(0x0A); this.SendData(0x00);
            this.SendCommand(0xC3);
            this.SendData(0x8A); this.SendData(0x2A);
            this.SendCommand(0xC4);
            this.SendData(0x8A); this.SendData(0xEE);

            this.SendCommand(0xC5); // VCOM
            this.SendData(0x0E);

            this.SendCommand(0x36); // MX, MY, RGB mode
            this.SendData(MADCTL_MX | MADCTL_MY | MADCTL_BGR);

            // ST7735R Gamma Sequence
            this.SendCommand(0xe0);
            this.SendData(0x0f); this.SendData(0x1a);
            this.SendData(0x0f); this.SendData(0x18);
            this.SendData(0x2f); this.SendData(0x28);
            this.SendData(0x20); this.SendData(0x22);
            this.SendData(0x1f); this.SendData(0x1b);
            this.SendData(0x23); this.SendData(0x37); this.SendData(0x00);

            this.SendData(0x07);
            this.SendData(0x02); this.SendData(0x10);
            this.SendCommand(0xe1);
            this.SendData(0x0f); this.SendData(0x1b);
            this.SendData(0x0f); this.SendData(0x17);
            this.SendData(0x33); this.SendData(0x2c);
            this.SendData(0x29); this.SendData(0x2e);
            this.SendData(0x30); this.SendData(0x30);
            this.SendData(0x39); this.SendData(0x3f);
            this.SendData(0x00); this.SendData(0x07);
            this.SendData(0x03); this.SendData(0x10);

            this.SendCommand(0x2a);
            this.SendData(0x00); this.SendData(0x00);
            this.SendData(0x00); this.SendData(0x7f);
            this.SendCommand(0x2b);
            this.SendData(0x00); this.SendData(0x00);
            this.SendData(0x00); this.SendData(0x9f);

            this.SendCommand(0xF0); //Enable test command
            this.SendData(0x01);
            this.SendCommand(0xF6); //Disable ram power save mode
            this.SendData(0x00);

            this.SetColorFormat(ColorFormat.Bgr16bit565); // Sets default color format to 65k color

            // Rotate
            this.SendCommand(ST7735_MADCTL);
            this.SendData(MADCTL_MV | MADCTL_MY);

            this.SendCommand(0x29); //Display on
            Thread.Sleep(50);
        }

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

        private void SetClip(int x, int y, int width, int height) {
            this.buffer4[1] = (byte)x;
            this.buffer4[3] = (byte)(x + width - 1);
            this.SendCommand(0x2A);
            this.SendData(this.buffer4);

            this.buffer4[1] = (byte)y;
            this.buffer4[3] = (byte)(y + height - 1);
            this.SendCommand(0x2B);
            this.SendData(this.buffer4);
        }

        public void SetColorFormat(ColorFormat colorFormat) {
            switch (colorFormat) {
                case ColorFormat.Bgr12bit444: // 4k colors mode
                    this.bitsPerPixel = ColorFormat.Bgr12bit444;
                    this.SendCommand(0x3A);
                    this.SendData(0x03);
                    break;
                case ColorFormat.Bgr16bit565: // 65k colors mode
                    this.bitsPerPixel = ColorFormat.Bgr16bit565;
                    this.SendCommand(0x3A);
                    this.SendData(0x05);
                    break;
            }
        }

        public void SetDrawWindow(int x, int y, int width, int height) {
            this.drawWindowX = x;
            this.drawWindowY = y;
            this.drawWindowWidth = width;
            this.drawWindowHeight = height;
        }

        public void PrepareToDraw() {
            this.SetClip(this.drawWindowX, this.drawWindowY, this.drawWindowWidth, this.drawWindowHeight);
            this.SendCommand(0x2C);
            this.control.Write(GpioPinValue.High);
        }

        public void DrawBuffer(byte[] buffer) => this.DrawBuffer(buffer, 0);

        public void DrawBuffer(byte[] buffer, int offset) {
            this.PrepareToDraw();

            this.spi.Write(buffer, offset, this.drawWindowHeight * this.drawWindowWidth * (int)this.bitsPerPixel / 8);
        }
    }
}
