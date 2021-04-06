using System;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.SolomonSystech.SSD1306 {
    public class SSD1306Controller {
        private readonly byte[] vram = new byte[128 * 64 / 8 + 1];
        private readonly byte[] buffer2 = new byte[2];
        private readonly I2cDevice i2c;

        public int Width => 128;
        public int Height => 64;

        public static I2cConnectionSettings GetConnectionSettings() => new I2cConnectionSettings(0x3C) {
            AddressFormat = I2cAddressFormat.SevenBit,
            BusSpeed = 400000,
        };

        public SSD1306Controller(I2cDevice i2c) {
            this.vram[0] = 0x40;
            this.i2c = i2c;

            this.Initialize();
        }

        private void Initialize() {
            this.SendCommand(0xAE); //turn off oled panel
            this.SendCommand(0x00); //set low column address
            this.SendCommand(0x10); //set high column address
            this.SendCommand(0x40); //set start line address
            this.SendCommand(0x81); //set contrast control register
            this.SendCommand(0xCF);
            this.SendCommand(0xA1); //set segment re-map 95 to 0
            this.SendCommand(0xA6); //set normal display
            this.SendCommand(0xA8); //set multiplex ratio(1 to 64)
            this.SendCommand(0x3F); //1/64 duty
            this.SendCommand(0xD3); //set display offset
            this.SendCommand(0x00); //not offset
            this.SendCommand(0xD5); //set display clock divide ratio/oscillator frequency
            this.SendCommand(0x80); //set divide ratio
            this.SendCommand(0xD9); //set pre-charge period
            this.SendCommand(0xF1);
            this.SendCommand(0xDA); //set com pins hardware configuration
            this.SendCommand(0x12);
            this.SendCommand(0xDB); //set vcomh
            this.SendCommand(0x40); //set startline 0x0
            this.SendCommand(0x8D); //set Charge Pump enable/disable
            this.SendCommand(0x14); //set(0x10) disable
            this.SendCommand(0xAF); //turn on oled panel
            this.SendCommand(0xC8); //mirror the screen

            // Mapping
            this.SendCommand(0x20);
            this.SendCommand(0x00);
            this.SendCommand(0x21);
            this.SendCommand(0);
            this.SendCommand(128 - 1);
            this.SendCommand(0x22);
            this.SendCommand(0);
            this.SendCommand(7);
        }

        public void Dispose() => this.i2c.Dispose();

        private void SendCommand(byte cmd) {
            this.buffer2[1] = cmd;
            this.i2c.Write(this.buffer2);
        }

        public void SetColorFormat(bool invert) => this.SendCommand((byte)(invert ? 0xA7 : 0xA6));

        public void SetPixel(int x, int y, bool color) {
            if (x < 0 || y < 0 || x >= this.Width || y >= this.Height) return;

            var index = (y / 8) * this.Width + x;

            if (color) {
                this.vram[1 + index] |= (byte)(1 << (y % 8));
            }
            else {
                this.vram[1 + index] &= (byte)(~(1 << (y % 8)));
            }
        }

        public void DrawBuffer(byte[] buffer) {
            var x = 0;
            var y = 0;

            for (var i = 0; i < buffer.Length; i += 2) {
                var color = (buffer[i + 1] << 8) | (buffer[i]);

                this.SetPixel(x++, y, color != 0);

                if (x == this.Width) {
                    x = 0;
                    y++;
                }
            }

            this.i2c.Write(this.vram);
        }

        public void DrawBufferNative(byte[] buffer, int offset, int count) {
            Array.Copy(buffer, offset, this.vram, 0, count);

            this.i2c.Write(this.vram);
        }
    }
}
