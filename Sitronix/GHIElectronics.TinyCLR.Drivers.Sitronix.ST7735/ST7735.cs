using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Sitronix.ST7735 {
    public enum ColorFormat {
        Bgr12bit444,
        Bgr16bit565
    }

    public class ST7735 {
        private const byte ST7735_MADCTL = 0x36;
        private const byte MADCTL_MY = 0x80;
        private const byte MADCTL_MX = 0x40;
        private const byte MADCTL_MV = 0x20;
        private const byte MADCTL_BGR = 0x08;

        private readonly SpiDevice spiBus;

        private readonly GpioPin resetPin;
        private readonly GpioPin controlPin;

        private readonly byte[] buffer1;
        private readonly byte[] buffer2;
        private readonly byte[] buffer4;

        private int drawWindowX = 0;
        private int drawWindowY = 0;
        private int drawWindowWidth = maxWidth;
        private int drawWindowHeight = maxHeight;

        private int bitsPerPixel = 16;

        /// <summary>
        /// The width of the display in pixels.
        /// </summary>
        public const int maxWidth = 160;

        /// <summary>
        /// The height of the display in pixels.
        /// </summary>
        public const int maxHeight = 128;

        public ST7735(int resetPin, int controlPin, int chipSelect, string spiId) {
            this.buffer1 = new byte[1];
            this.buffer2 = new byte[2];
            this.buffer4 = new byte[4];

            var GPIO = GpioController.GetDefault();

            this.controlPin = GPIO.OpenPin(controlPin);
            this.controlPin.SetDriveMode(GpioPinDriveMode.Output);

            this.resetPin = GPIO.OpenPin(resetPin);
            this.resetPin.SetDriveMode(GpioPinDriveMode.Output);

            var spiSettings = new SpiConnectionSettings(chipSelect) {
                Mode = SpiMode.Mode3,
                ClockFrequency = 12000000,
                DataBitLength = 8
            };
            
            this.spiBus = SpiController.FromName(spiId).GetDevice(spiSettings);

            this.Reset();
            this.InitializeST7735();
        }

        private void InitializeST7735() {
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
            this.controlPin.Write(GpioPinValue.Low);
            this.spiBus.Write(this.buffer1);
        }

        public void SendData(byte data) {
            this.buffer1[0] = data;
            this.SendData(this.buffer1);
        }

        public void SendData(byte[] data) {
            this.controlPin.Write(GpioPinValue.High);
            this.spiBus.Write(data);
        }

        private void Reset() {
            this.resetPin.Write(GpioPinValue.Low);
            Thread.Sleep(50);
            this.resetPin.Write(GpioPinValue.High);
            Thread.Sleep(200);
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
                    this.bitsPerPixel = 12;
                    this.SendCommand(0x3A);
                    this.SendData(0x03);
                    break;
                case ColorFormat.Bgr16bit565: // 65k colors mode
                    this.bitsPerPixel = 16;
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
            this.controlPin.Write(GpioPinValue.High);
        }

        public void DrawBuffer(byte[] buffer) => this.DrawBuffer(buffer, 0);
        
        public void DrawBuffer(byte[] buffer, int offset) {
            this.PrepareToDraw();
            this.WriteData(buffer, offset);
        }

        protected virtual void WriteData(byte[] buffer, int offset) => this.spiBus.Write(buffer, offset, this.drawWindowHeight * this.drawWindowWidth * this.bitsPerPixel / 8);
    }
}
