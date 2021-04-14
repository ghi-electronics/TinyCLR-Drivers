using System;

namespace GHIElectronics.TinyCLR.Drivers.BasicGraphic {
    public enum ColorFormat {
        Rgb565 = 0,
        OneBpp = 1
    }
    public class BasicGraphics {
        private ColorFormat colorFormat;
        private byte[] buffer;
        private int width;
        private int height;

        public int Width { get => this.width; set => this.width = value; }
        public int Height { get => this.height; set => this.height = value; }
        public ColorFormat ColorFormat { get => this.ColorFormat; set => this.ColorFormat = value; }
        public byte[] Buffer => this.buffer;
        public BasicGraphics() {

        }

        public BasicGraphics(uint width, uint height, ColorFormat colorFormat) {
            this.colorFormat = colorFormat;
            this.width = (int)width;
            this.height = (int)height;

            if (this.colorFormat == ColorFormat.Rgb565) {
                this.buffer = new byte[this.width * this.height * 2];
            }
            else if (this.colorFormat == ColorFormat.OneBpp) {
                this.buffer = new byte[this.width * this.height  / 8];
            }

        }

        public virtual void Clear() {
            if (this.buffer != null)
                Array.Clear(this.buffer, 0, this.buffer.Length);
        }
        public virtual void SetPixel(int x, int y, uint color) {
            if (this.buffer == null) {
                throw new Exception("Buffer null.");
            }

            if (x < 0 || y < 0 || x >= this.width || y >= this.height) return;

            if (this.colorFormat == ColorFormat.Rgb565) {
                var index = (y * this.width + x) * 2;
                var clr = color;

                this.buffer[index + 0] = (byte)(((clr & 0b0000_0000_0000_0000_0001_1100_0000_0000) >> 5) | ((clr & 0b0000_0000_0000_0000_0000_0000_1111_1000) >> 3));
                this.buffer[index + 1] = (byte)(((clr & 0b0000_0000_1111_1000_0000_0000_0000_0000) >> 16) | ((clr & 0b0000_0000_0000_0000_1110_0000_0000_0000) >> 13));

            }
            else if (this.colorFormat == ColorFormat.OneBpp) {
                var index = (y / 8) * this.width + x;

                if (color != 0) {
                    this.buffer[index] |= (byte)(1 << (y % 8));
                }
                else {
                    this.buffer[index] &= (byte)(~(1 << (y % 8)));
                }
            }
            else {
                throw new Exception("Only 16bpp or 1bpp supported.");
            }

        }
        public void DrawLine(uint color, int x0, int y0, int x1, int y1) {

            var xLength = x1 - x0;
            var yLength = y1 - y0;
            int stepx, stepy;

            if (yLength < 0) { yLength = -yLength; stepy = -1; } else { stepy = 1; }
            if (xLength < 0) { xLength = -xLength; stepx = -1; } else { stepx = 1; }
            yLength <<= 1;                                  // yLength is now 2 * yLength
            xLength <<= 1;                                  // xLength is now 2 * xLength

            this.SetPixel(x0, y0, color);
            if (xLength > yLength) {
                var fraction = yLength - (xLength >> 1);    // same as 2 * yLength - xLength
                while (x0 != x1) {
                    if (fraction >= 0) {
                        y0 += stepy;
                        fraction -= xLength;                // same as fraction -= 2 * xLength
                    }
                    x0 += stepx;
                    fraction += yLength;                    // same as fraction -= 2 * yLength
                    this.SetPixel(x0, y0, color);
                }
            }
            else {
                var fraction = xLength - (yLength >> 1);
                while (y0 != y1) {
                    if (fraction >= 0) {
                        x0 += stepx;
                        fraction -= yLength;
                    }
                    y0 += stepy;
                    fraction += xLength;
                    this.SetPixel(x0, y0, color);
                }
            }
        }

        public void DrawRectangle(uint color, int x, int y, int width, int height) {

            if (width < 0) return;
            if (height < 0) return;

            for (var i = x; i < x + width; i++) {
                this.SetPixel(i, y, color);
                this.SetPixel(i, y + height - 1, color);
            }

            for (var i = y; i < y + height; i++) {
                this.SetPixel(x, i, color);
                this.SetPixel(x + width - 1, i, color);
            }
        }

        public void DrawCircle(uint color, int x, int y, int radius) {

            if (radius <= 0) return;

            var centerX = x;
            var centerY = y;

            var f = 1 - radius;
            var ddFX = 1;
            var ddFY = -2 * radius;
            var dX = 0;
            var dY = radius;

            this.SetPixel(centerX, centerY + radius, color);
            this.SetPixel(centerX, centerY - radius, color);
            this.SetPixel(centerX + radius, centerY, color);
            this.SetPixel(centerX - radius, centerY, color);

            while (dX < dY) {
                if (f >= 0) {
                    dY--;
                    ddFY += 2;
                    f += ddFY;
                }

                dX++;
                ddFX += 2;
                f += ddFX;

                this.SetPixel(centerX + dX, centerY + dY, color);
                this.SetPixel(centerX - dX, centerY + dY, color);
                this.SetPixel(centerX + dX, centerY - dY, color);
                this.SetPixel(centerX - dX, centerY - dY, color);

                this.SetPixel(centerX + dY, centerY + dX, color);
                this.SetPixel(centerX - dY, centerY + dX, color);
                this.SetPixel(centerX + dY, centerY - dX, color);
                this.SetPixel(centerX - dY, centerY - dX, color);
            }
        }

        public void DrawString(string text, uint color, int x, int y) => this.DrawText(text, color, x, y, 1, 1);
        public void DrawText(string text, uint color, int x, int y, int hScale, int vScale) {
            if (hScale == 0 || vScale == 0) throw new ArgumentNullException();
            var originalX = x;

            for (var i = 0; i < text.Length; i++) {
                if (text[i] >= 32) {
                    this.DrawCharacter(text[i], color, x, y, hScale, vScale);
                    x += (6 * hScale);
                }
                else {
                    if (text[i] == '\n') {
                        y += (9 * vScale);
                        x = originalX;
                    }
                    if (text[i] == '\r')
                        x = originalX;
                }
            }
        }

        public void DrawCharacter(char character, uint color, int x, int y, int hScale, int vScale) {
            var index = 5 * (character - 32);

            for (var horizontalFontSize = 0; horizontalFontSize < 5; horizontalFontSize++) {
                for (var hs = 0; hs < hScale; hs++) {
                    for (var verticleFontSize = 0; verticleFontSize < 8; verticleFontSize++) {
                        for (var vs = 0; vs < vScale; vs++) {
                            if ((this.ghiGHIMono8x5[index + horizontalFontSize] & (1 << verticleFontSize)) != 0)
                                this.SetPixel(x + (horizontalFontSize * hScale) + hs, y + (verticleFontSize * vScale) + vs, color);
                        }
                    }
                }
            }
        }

        readonly byte[] ghiGHIMono8x5 = new byte[95 * 5] {
            0x00, 0x00, 0x00, 0x00, 0x00, /* Space	0x20 */
            0x00, 0x00, 0x4f, 0x00, 0x00, /* ! */
            0x00, 0x07, 0x00, 0x07, 0x00, /* " */
            0x14, 0x7f, 0x14, 0x7f, 0x14, /* # */
            0x24, 0x2a, 0x7f, 0x2a, 0x12, /* $ */
            0x23, 0x13, 0x08, 0x64, 0x62, /* % */
            0x36, 0x49, 0x55, 0x22, 0x20, /* & */
            0x00, 0x05, 0x03, 0x00, 0x00, /* ' */
            0x00, 0x1c, 0x22, 0x41, 0x00, /* ( */
            0x00, 0x41, 0x22, 0x1c, 0x00, /* ) */
            0x14, 0x08, 0x3e, 0x08, 0x14, /* // */
            0x08, 0x08, 0x3e, 0x08, 0x08, /* + */
            0x50, 0x30, 0x00, 0x00, 0x00, /* , */
            0x08, 0x08, 0x08, 0x08, 0x08, /* - */
            0x00, 0x60, 0x60, 0x00, 0x00, /* . */
            0x20, 0x10, 0x08, 0x04, 0x02, /* / */
            0x3e, 0x51, 0x49, 0x45, 0x3e, /* 0		0x30 */
            0x00, 0x42, 0x7f, 0x40, 0x00, /* 1 */
            0x42, 0x61, 0x51, 0x49, 0x46, /* 2 */
            0x21, 0x41, 0x45, 0x4b, 0x31, /* 3 */
            0x18, 0x14, 0x12, 0x7f, 0x10, /* 4 */
            0x27, 0x45, 0x45, 0x45, 0x39, /* 5 */
            0x3c, 0x4a, 0x49, 0x49, 0x30, /* 6 */
            0x01, 0x71, 0x09, 0x05, 0x03, /* 7 */
            0x36, 0x49, 0x49, 0x49, 0x36, /* 8 */
            0x06, 0x49, 0x49, 0x29, 0x1e, /* 9 */
            0x00, 0x36, 0x36, 0x00, 0x00, /* : */
            0x00, 0x56, 0x36, 0x00, 0x00, /* ; */
            0x08, 0x14, 0x22, 0x41, 0x00, /* < */
            0x14, 0x14, 0x14, 0x14, 0x14, /* = */
            0x00, 0x41, 0x22, 0x14, 0x08, /* > */
            0x02, 0x01, 0x51, 0x09, 0x06, /* ? */
            0x3e, 0x41, 0x5d, 0x55, 0x1e, /* @		0x40 */
            0x7e, 0x11, 0x11, 0x11, 0x7e, /* A */
            0x7f, 0x49, 0x49, 0x49, 0x36, /* B */
            0x3e, 0x41, 0x41, 0x41, 0x22, /* C */
            0x7f, 0x41, 0x41, 0x22, 0x1c, /* D */
            0x7f, 0x49, 0x49, 0x49, 0x41, /* E */
            0x7f, 0x09, 0x09, 0x09, 0x01, /* F */
            0x3e, 0x41, 0x49, 0x49, 0x7a, /* G */
            0x7f, 0x08, 0x08, 0x08, 0x7f, /* H */
            0x00, 0x41, 0x7f, 0x41, 0x00, /* I */
            0x20, 0x40, 0x41, 0x3f, 0x01, /* J */
            0x7f, 0x08, 0x14, 0x22, 0x41, /* K */
            0x7f, 0x40, 0x40, 0x40, 0x40, /* L */
            0x7f, 0x02, 0x0c, 0x02, 0x7f, /* M */
            0x7f, 0x04, 0x08, 0x10, 0x7f, /* N */
            0x3e, 0x41, 0x41, 0x41, 0x3e, /* O */
            0x7f, 0x09, 0x09, 0x09, 0x06, /* P		0x50 */
            0x3e, 0x41, 0x51, 0x21, 0x5e, /* Q */
            0x7f, 0x09, 0x19, 0x29, 0x46, /* R */
            0x26, 0x49, 0x49, 0x49, 0x32, /* S */
            0x01, 0x01, 0x7f, 0x01, 0x01, /* T */
            0x3f, 0x40, 0x40, 0x40, 0x3f, /* U */
            0x1f, 0x20, 0x40, 0x20, 0x1f, /* V */
            0x3f, 0x40, 0x38, 0x40, 0x3f, /* W */
            0x63, 0x14, 0x08, 0x14, 0x63, /* X */
            0x07, 0x08, 0x70, 0x08, 0x07, /* Y */
            0x61, 0x51, 0x49, 0x45, 0x43, /* Z */
            0x00, 0x7f, 0x41, 0x41, 0x00, /* [ */
            0x02, 0x04, 0x08, 0x10, 0x20, /* \ */
            0x00, 0x41, 0x41, 0x7f, 0x00, /* ] */
            0x04, 0x02, 0x01, 0x02, 0x04, /* ^ */
            0x40, 0x40, 0x40, 0x40, 0x40, /* _ */
            0x00, 0x00, 0x03, 0x05, 0x00, /* `		0x60 */
            0x20, 0x54, 0x54, 0x54, 0x78, /* a */
            0x7F, 0x44, 0x44, 0x44, 0x38, /* b */
            0x38, 0x44, 0x44, 0x44, 0x44, /* c */
            0x38, 0x44, 0x44, 0x44, 0x7f, /* d */
            0x38, 0x54, 0x54, 0x54, 0x18, /* e */
            0x04, 0x04, 0x7e, 0x05, 0x05, /* f */
            0x08, 0x54, 0x54, 0x54, 0x3c, /* g */
            0x7f, 0x08, 0x04, 0x04, 0x78, /* h */
            0x00, 0x44, 0x7d, 0x40, 0x00, /* i */
            0x20, 0x40, 0x44, 0x3d, 0x00, /* j */
            0x7f, 0x10, 0x28, 0x44, 0x00, /* k */
            0x00, 0x41, 0x7f, 0x40, 0x00, /* l */
            0x7c, 0x04, 0x7c, 0x04, 0x78, /* m */
            0x7c, 0x08, 0x04, 0x04, 0x78, /* n */
            0x38, 0x44, 0x44, 0x44, 0x38, /* o */
            0x7c, 0x14, 0x14, 0x14, 0x08, /* p		0x70 */
            0x08, 0x14, 0x14, 0x14, 0x7c, /* q */
            0x7c, 0x08, 0x04, 0x04, 0x08, /* r */
            0x48, 0x54, 0x54, 0x54, 0x24, /* s */
            0x04, 0x04, 0x3f, 0x44, 0x44, /* t */
            0x3c, 0x40, 0x40, 0x20, 0x7c, /* u */
            0x1c, 0x20, 0x40, 0x20, 0x1c, /* v */
            0x3c, 0x40, 0x30, 0x40, 0x3c, /* w */
            0x44, 0x28, 0x10, 0x28, 0x44, /* x */
            0x0c, 0x50, 0x50, 0x50, 0x3c, /* y */
            0x44, 0x64, 0x54, 0x4c, 0x44, /* z */
            0x08, 0x36, 0x41, 0x41, 0x00, /* { */
            0x00, 0x00, 0x77, 0x00, 0x00, /* | */
            0x00, 0x41, 0x41, 0x36, 0x08, /* } */
            0x08, 0x08, 0x2a, 0x1c, 0x08  /* ~ */
        };

    }
}
