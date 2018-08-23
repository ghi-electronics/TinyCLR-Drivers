using System.Threading;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.Sitronix.ST7735 {
    public enum ColorFormat {
        BGR12bit444,
        BGR16bit565
    }

    public class ST7735 {
        private const byte ST7735_MADCTL = 0x36;
        private const byte MADCTL_MY = 0x80;
        private const byte MADCTL_MX = 0x40;
        private const byte MADCTL_MV = 0x20;
        private const byte MADCTL_BGR = 0x08;

        private readonly SpiController spiBusContoller;
        private readonly SpiDevice spiBus;

        private readonly GpioPin resetPin;
        private readonly GpioPin controlPin;

        private readonly byte[] singleByteBuffer;
        private readonly byte[] twoByteBuffer;
        private readonly byte[] wordBuffer;

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
            this.singleByteBuffer = new byte[1];
            this.twoByteBuffer = new byte[2];
            this.wordBuffer = new byte[4];

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

            this.spiBusContoller = SpiController.FromName(spiId);
            this.spiBus = this.spiBusContoller.GetDevice(spiSettings);

            this.Reset();
            this.InitializeST7735();
        }

        private void InitializeST7735() {

            this.WriteCommand(0x01); //reset
            Thread.Sleep(120);

            this.WriteCommand(0x11); //Sleep exit 
            Thread.Sleep(200);

            // ST7735R Frame Rate
            this.WriteCommand(0xB1);
            this.WriteData(0x01); this.WriteData(0x2C); this.WriteData(0x2D);
            this.WriteCommand(0xB2);
            this.WriteData(0x01); this.WriteData(0x2C); this.WriteData(0x2D);
            this.WriteCommand(0xB3);
            this.WriteData(0x01); this.WriteData(0x2C); this.WriteData(0x2D);
            this.WriteData(0x01); this.WriteData(0x2C); this.WriteData(0x2D);

            this.WriteCommand(0xB4); // Column inversion 
            this.WriteData(0x07);

            // ST7735R Power Sequence
            this.WriteCommand(0xC0);
            this.WriteData(0xA2); this.WriteData(0x02); this.WriteData(0x84);
            this.WriteCommand(0xC1); this.WriteData(0xC5);
            this.WriteCommand(0xC2);
            this.WriteData(0x0A); this.WriteData(0x00);
            this.WriteCommand(0xC3);
            this.WriteData(0x8A); this.WriteData(0x2A);
            this.WriteCommand(0xC4);
            this.WriteData(0x8A); this.WriteData(0xEE);

            this.WriteCommand(0xC5); // VCOM 
            this.WriteData(0x0E);

            this.WriteCommand(0x36); // MX, MY, RGB mode
            this.WriteData(MADCTL_MX | MADCTL_MY | MADCTL_BGR);

            // ST7735R Gamma Sequence
            this.WriteCommand(0xe0);
            this.WriteData(0x0f); this.WriteData(0x1a);
            this.WriteData(0x0f); this.WriteData(0x18);
            this.WriteData(0x2f); this.WriteData(0x28);
            this.WriteData(0x20); this.WriteData(0x22);
            this.WriteData(0x1f); this.WriteData(0x1b);
            this.WriteData(0x23); this.WriteData(0x37); this.WriteData(0x00);

            this.WriteData(0x07);
            this.WriteData(0x02); this.WriteData(0x10);
            this.WriteCommand(0xe1);
            this.WriteData(0x0f); this.WriteData(0x1b);
            this.WriteData(0x0f); this.WriteData(0x17);
            this.WriteData(0x33); this.WriteData(0x2c);
            this.WriteData(0x29); this.WriteData(0x2e);
            this.WriteData(0x30); this.WriteData(0x30);
            this.WriteData(0x39); this.WriteData(0x3f);
            this.WriteData(0x00); this.WriteData(0x07);
            this.WriteData(0x03); this.WriteData(0x10);

            this.WriteCommand(0x2a);
            this.WriteData(0x00); this.WriteData(0x00);
            this.WriteData(0x00); this.WriteData(0x7f);
            this.WriteCommand(0x2b);
            this.WriteData(0x00); this.WriteData(0x00);
            this.WriteData(0x00); this.WriteData(0x9f);

            this.WriteCommand(0xF0); //Enable test command  
            this.WriteData(0x01);
            this.WriteCommand(0xF6); //Disable ram power save mode 
            this.WriteData(0x00);

            this.SetColorFormat(ColorFormat.BGR16bit565); // Sets default color format to 65k color

            // Rotate
            this.WriteCommand(ST7735_MADCTL);
            this.WriteData(MADCTL_MV | MADCTL_MY);

            this.WriteCommand(0x29); //Display on
            Thread.Sleep(50);
        }

        private void WriteCommand(byte command) {
            this.singleByteBuffer[0] = command;
            this.controlPin.Write(GpioPinValue.Low);
            this.spiBus.Write(this.singleByteBuffer);
        }

        private void WriteData(byte data) {
            this.singleByteBuffer[0] = data;
            this.WriteData(this.singleByteBuffer);
        }

        private void WriteData(byte[] data) {
            this.controlPin.Write(GpioPinValue.High);
            this.spiBus.Write(data);
        }

        private void Reset() {
            this.resetPin.Write(GpioPinValue.Low);
            Thread.Sleep(50);
            this.resetPin.Write(GpioPinValue.High);
            Thread.Sleep(200);
        }

        /*public void Clear() {
            SetClip(0, 0, 160, 128);
            WriteCommand(0x2C);

            for (var i = 0; i < 128 / 16; i++)
                WriteData(clearBuffer);
        }*/

        private void SetClip(int x, int y, int width, int height) {
            this.wordBuffer[1] = (byte)x;
            this.wordBuffer[3] = (byte)(x + width - 1);
            this.WriteCommand(0x2A);
            this.WriteData(this.wordBuffer);

            this.wordBuffer[1] = (byte)y;
            this.wordBuffer[3] = (byte)(y + height - 1);
            this.WriteCommand(0x2B);
            this.WriteData(this.wordBuffer);
        }

        public void SetColorFormat(ColorFormat colorFormat) {
            switch (colorFormat) {
                case ColorFormat.BGR12bit444: // 4k colors mode 
                    this.bitsPerPixel = 12;
                    this.WriteCommand(0x3A);
                    this.WriteData(0x03);
                    break;
                case ColorFormat.BGR16bit565: // 65k colors mode
                    this.bitsPerPixel = 16;
                    this.WriteCommand(0x3A); 
                    this.WriteData(0x05);
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
            this.WriteCommand(0x2C);
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
