using System;
using System.Text;

namespace GHIElectronics.TinyCLR.Drivers.BasicGraphics {
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
                this.buffer = new byte[this.width * this.height / 8];
            }
        }

        public virtual void Clear() {
            if (this.buffer != null)
                Array.Clear(this.buffer, 0, this.buffer.Length);
        }
        public virtual void SetPixel(int x, int y, uint color) {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) return;

            if (this.colorFormat == ColorFormat.Rgb565) {
                var index = (y * this.width + x) * 2;
                var clr = color;

                this.buffer[index + 0] = (byte)(((clr & 0b0000_0000_0000_0000_0001_1100_0000_0000) >> 5) | ((clr & 0b0000_0000_0000_0000_0000_0000_1111_1000) >> 3));
                this.buffer[index + 1] = (byte)(((clr & 0b0000_0000_1111_1000_0000_0000_0000_0000) >> 16) | ((clr & 0b0000_0000_0000_0000_1110_0000_0000_0000) >> 13));

            }
            else if (this.colorFormat == ColorFormat.OneBpp) {
                var index = (y >> 3) * this.width + x;
                if (color != 0) {
                    this.buffer[index] |= (byte)(1 << (y & 7));
                }
                else {
                    this.buffer[index] &= (byte)(~(1 << (y & 7)));
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
        public void DrawTinyString(string text, uint color, int x, int y) => this.DrawTinyString(text, color, x, y, false);
        public void DrawTinyString(string text, uint color, int x, int y, bool clear) {
            for (var i = 0; i < text.Length; i++) {
                this.DrawTinyCharacter(text[i], color, x, y, clear);
                x += 6;
                if (clear) {
                    // clear the space between chars
                    for (var s = 0; s < 5; s++) {
                        this.SetPixel(x - 1, y + s, 0);
                    }
                }
            }
        }
        public void DrawString(string text, uint color, int x, int y) => this.DrawString(text, color, x, y, 1, 1);
        public void DrawString(string text, uint color, int x, int y, int hScale, int vScale) {
            if (hScale == 0 || vScale == 0) throw new ArgumentNullException();
            var originalX = x;
            for (var i = 0; i < text.Length; i++) {
                if (text[i] >= 32) {
                    this.DrawCharacter(text[i], color, x, y, hScale, vScale);
                    x += 6;
                }
                else {
                    if (text[i] == '\n') {
                        y += 9;
                        x = originalX;
                    }
                    if (text[i] == '\r')
                        x = originalX;
                }
            }
        }
        public void DrawTinyCharacter(char character, uint color, int x, int y) => this.DrawTinyCharacter(character, color, x, y, false);
        public void DrawTinyCharacter(char character, uint color, int x, int y, bool clear) {
            var index = 5 * (character - 32);

            for (var col = 0; col < 5; col++) {
                var fontCol = this.mono5x5[index + col];
                for (var row = 0; row < 5; row++) {
                    if ((fontCol & (1 << (4 - row))) != 0)
                        this.SetPixel(x + row, y + col, color);
                    else
                        if (clear)
                        this.SetPixel(x + row, y + col, 0);

                }
            }
        }
        public void DrawCharacter(char character, uint color, int x, int y) => this.DrawCharacter(character, color, x, y, 1, 1);

        public void DrawCharacter(char character, uint color, int x, int y, int hScale, int vScale) {
            var index = 5 * (character - 32);
            if (hScale != 1 || vScale != 1) {
                for (var horizontalFontSize = 0; horizontalFontSize < 5; horizontalFontSize++) {
                    var sx = x + horizontalFontSize;
                    var fontRow = this.mono8x5[index + horizontalFontSize];
                    for (var verticleFontSize = 0; verticleFontSize < 8; verticleFontSize++) {
                        if ((fontRow & (1 << verticleFontSize)) != 0) this.SetPixel(sx, y + verticleFontSize, hScale, vScale, color);
                    }
                }
            }
            else {
                for (var horizontalFontSize = 0; horizontalFontSize < 5; horizontalFontSize++) {
                    var sx = x + horizontalFontSize;
                    var fontRow = this.mono8x5[index + horizontalFontSize];
                    for (var verticleFontSize = 0; verticleFontSize < 8; verticleFontSize++) {
                        if ((fontRow & (1 << verticleFontSize)) != 0) this.SetPixel(sx, y + verticleFontSize, color);
                    }
                }
            }
        }

        private void SetPixel(int x, int y, int hScale, int vScale, uint color) {
            x *= hScale;
            y *= vScale;
            for (var ix = 0; ix < hScale; ix++) {
                for (var iy = 0; iy < vScale; iy++) {
                    this.SetPixel(x + ix, y + iy, color);
                }
            }
        }

        public static uint ColorFromRgb(byte red, byte green, byte blue) => (uint)(red << 16 | green << 8 | blue << 0);

        public void DrawImage(Image img, int x, int y) {
            var index = 0;
            for (var vsize = 0; vsize < img.Height; vsize++) {
                for (var hsize = 0; hsize < img.Width; hsize++) {
                    this.SetPixel(x + hsize, y + vsize, img.Data[index++]);
                }
            }
        }


        readonly byte[] mono5x5 = new byte[95 * 5] {
            // font from lancaster/microbit
            // each byte is a column
            // there are 3 bits that are not clear on what they do! We will just ignore.
            // font should modified to have variable width and use first unused bit for width
            0x00, 0x00, 0x00, 0x00, 0x00,/* Space	0x20 */
            0x08, 0x08, 0x08, 0x00, 0x08,/* ! */
            0x0a, 0x4a, 0x40, 0x00, 0x00,/* " */
            0x0a, 0x5f, 0xea, 0x5f, 0xea,/* # */
            0x0e, 0xd9, 0x2e, 0xd3, 0x6e,/* $ */
            0x19, 0x32, 0x44, 0x89, 0x33,/* % */
            0x0c, 0x92, 0x4c, 0x92, 0x4d,/* & */
            0x08, 0x08, 0x00, 0x00, 0x00,/* ' */
            0x04, 0x88, 0x08, 0x08, 0x04,/* ( */
            0x08, 0x04, 0x84, 0x84, 0x88,/* ) */
            0x00, 0x0a, 0x44, 0x8a, 0x40,/* // */
            0x00, 0x04, 0x8e, 0xc4, 0x80,/* + */ 
            0x00, 0x00, 0x00, 0x04, 0x88,/* , */
            0x00, 0x00, 0x0e, 0xc0, 0x00,/* - */
            0x00, 0x00, 0x00, 0x08, 0x00,/* . */
            0x01, 0x22, 0x44, 0x88, 0x10,/* / */
            0x0c, 0x92, 0x52, 0x52, 0x4c,/* 0		0x30 */
            0x04, 0x8c, 0x84, 0x84, 0x8e,/* 1 */
            0x1c, 0x82, 0x4c, 0x90, 0x1e,/* 2 */
            0x1e, 0xc2, 0x44, 0x92, 0x4c,/* 3 */
            0x06, 0xca, 0x52, 0x5f, 0xe2,/* 4 */
            0x1f, 0xf0, 0x1e, 0xc1, 0x3e,/* 5 */
            0x02, 0x44, 0x8e, 0xd1, 0x2e,/* 6 */
            0x1f, 0xe2, 0x44, 0x88, 0x10,/* 7 */
            0x0e, 0xd1, 0x2e, 0xd1, 0x2e,/* 8 */
            0x0e, 0xd1, 0x2e, 0xc4, 0x88,/* 9 */
            0x00, 0x08, 0x00, 0x08, 0x00,/* : */
            0x00, 0x04, 0x80, 0x04, 0x88,/* ; */
            0x02, 0x44, 0x88, 0x04, 0x82,/* < */
            0x00, 0x0e, 0xc0, 0x0e, 0xc0,/* = */
            0x08, 0x04, 0x82, 0x44, 0x88,/* > */
            0x0e, 0xd1, 0x26, 0xc0, 0x04,/* ? */
            0x0e, 0xd1, 0x35, 0xb3, 0x6c,/* @		0x40 */
            0x0c, 0x92, 0x5e, 0xd2, 0x52,/* A */
            0x1c, 0x92, 0x5c, 0x92, 0x5c,/* B */
            0x0e, 0xd0, 0x10, 0x10, 0x0e,/* C */
            0x1c, 0x92, 0x52, 0x52, 0x5c,/* D */
            0x1e, 0xd0, 0x1c, 0x90, 0x1e,/* E */
            0x1e, 0xd0, 0x1c, 0x90, 0x10,/* F */
            0x0e, 0xd0, 0x13, 0x71, 0x2e,/* G */
            0x12, 0x52, 0x5e, 0xd2, 0x52,/* H */
            0x1c, 0x88, 0x08, 0x08, 0x1c,/* I */
            0x1f, 0xe2, 0x42, 0x52, 0x4c,/* J */
            0x12, 0x54, 0x98, 0x14, 0x92,/* K */
            0x10, 0x10, 0x10, 0x10, 0x1e,/* L */
            0x11, 0x3b, 0x75, 0xb1, 0x31,/* M */
            0x11, 0x39, 0x35, 0xb3, 0x71,/* N */
            0x0c, 0x92, 0x52, 0x52, 0x4c,/* O */
            0x1c, 0x92, 0x5c, 0x90, 0x10,/* P		0x50 */ 
            0x0c, 0x92, 0x52, 0x4c, 0x86,/* Q */ 
            0x1c, 0x92, 0x5c, 0x92, 0x51,/* R */ 
            0x0e, 0xd0, 0x0c, 0x82, 0x5c,/* S */ 
            0x1f, 0xe4, 0x84, 0x84, 0x84,/* T */ 
            0x12, 0x52, 0x52, 0x52, 0x4c,/* U */
            0x11, 0x31, 0x31, 0x2a, 0x44,/* V */
            0x11, 0x31, 0x35, 0xbb, 0x71,/* W */ 
            0x12, 0x52, 0x4c, 0x92, 0x52,/* X */
            0x11, 0x2a, 0x44, 0x84, 0x84,/* Y */
            0x1e, 0xc4, 0x88, 0x10, 0x1e,/* Z */ 
            0x0e, 0xc8, 0x08, 0x08, 0x0e,/* [ */ 
            0x10, 0x08, 0x04, 0x82, 0x41,/* \ */
            0x0e, 0xc2, 0x42, 0x42, 0x4e,/* ] */
            0x04, 0x8a, 0x40, 0x00, 0x00,/* ^ */
            0x00, 0x00, 0x00, 0x00, 0x1f,/* _ */
            0x08, 0x04, 0x80, 0x00, 0x00,/* `		0x60 */ 
            0x00, 0x0e, 0xd2, 0x52, 0x4f,/* a */ 
            0x10, 0x10, 0x1c, 0x92, 0x5c,/* b */
            0x00, 0x0e, 0xd0, 0x10, 0x0e,/* c */ 
            0x02, 0x42, 0x4e, 0xd2, 0x4e,/* d */
            0x0c, 0x92, 0x5c, 0x90, 0x0e,/* e */
            0x06, 0xc8, 0x1c, 0x88, 0x08,/* f */ 
            0x0e, 0xd2, 0x4e, 0xc2, 0x4c,/* g */ 
            0x10, 0x10, 0x1c, 0x92, 0x52,/* h */
            0x08, 0x00, 0x08, 0x08, 0x08,/* i */
            0x02, 0x40, 0x02, 0x42, 0x4c,/* j */ 
            0x10, 0x14, 0x98, 0x14, 0x92,/* k */ 
            0x08, 0x08, 0x08, 0x08, 0x06,/* l */
            0x00, 0x1b, 0x75, 0xb1, 0x31,/* m */
            0x00, 0x1c, 0x92, 0x52, 0x52,/* n */ 
            0x00, 0x0c, 0x92, 0x52, 0x4c,/* o */ 
            0x00, 0x1c, 0x92, 0x5c, 0x90,/* p		0x70 */ 
            0x00, 0x0e, 0xd2, 0x4e, 0xc2,/* q */ 
            0x00, 0x0e, 0xd0, 0x10, 0x10,/* r */
            0x00, 0x06, 0xc8, 0x04, 0x98,/* s */ 
            0x08, 0x08, 0x0e, 0xc8, 0x07,/* t */ 
            0x00, 0x12, 0x52, 0x52, 0x4f,/* u */
            0x00, 0x11, 0x31, 0x2a, 0x44,/* v */
            0x00, 0x11, 0x31, 0x35, 0xbb,/* w */ 
            0x00, 0x12, 0x4c, 0x8c, 0x92,/* x */ 
            0x00, 0x11, 0x2a, 0x44, 0x98,/* y */
            0x00, 0x1e, 0xc4, 0x88, 0x1e,/* z */ 
            0x06, 0xc4, 0x8c, 0x84, 0x86,/* { */ 
            0x08, 0x08, 0x08, 0x08, 0x08,/* | */ 
            0x18, 0x08, 0x0c, 0x88, 0x18,/* } */ 
            0x00, 0x00, 0x0c, 0x83, 0x60}/* ~ */;

        readonly byte[] mono8x5 = new byte[95 * 5] {
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
    public class Image {
        public enum Transform {
            None,
            FlipHorizontal,
            FlipVertical,
            Rotate90,
            Rotate180,
            Rotate270,
        }
        public int Height { get; internal set; }
        public int Width { get; internal set; }
        public byte[] Data { get; internal set; }

        public Image(string img, int width, int height) : this(img, width, height, 1, 1, Transform.None) { }
        public Image(string img, int width, int height, int hScale, int vScale, Transform transform) {
            var data = Encoding.UTF8.GetBytes(img);

            for (var x = 0; x < data.Length; x++) {
                if (data[x] == ' ') {
                    data[x] = 0;
                }
            }

            this.CreateImage(data, width, height, hScale, vScale, transform);
        }
        public Image(byte[] data, int width, int height) : this(data, width, height, 1, 1, Transform.None) { }

        public Image(byte[] data, int width, int height, int hScale, int vScale, Transform transform) => this.CreateImage(data, width, height, hScale, vScale, transform);

        private void CreateImage(byte[] data, int width, int height, int hScale, int vScale, Transform transform) {

            if (width * height != data.Length) throw new Exception("Incorrect image data size");

            this.Height = height * vScale;
            this.Width = width * hScale;

            this.Data = new byte[this.Width * this.Height];

            for (var x = 0; x < this.Width; x++) {
                for (var y = 0; y < this.Height; y++) {
                    switch (transform) {
                        case Transform.None:
                            this.Data[y * this.Width + x] = data[y / vScale * width + x / hScale];
                            break;
                        case Transform.FlipHorizontal:
                            this.Data[y * this.Width + (this.Width - x - 1)] = data[y / vScale * width + x / hScale];
                            break;
                        case Transform.FlipVertical:
                            this.Data[(this.Height - y - 1) * this.Width + x] = data[y / vScale * width + x / hScale];
                            break;
                        case Transform.Rotate90:
                            this.Data[x * this.Height + this.Height - y - 1] = data[y / vScale * width + x / hScale];
                            break;
                        case Transform.Rotate180:
                            this.Data[(this.Height - y - 1) * this.Width + (this.Width - x - 1)] = data[y / vScale * width + x / hScale];
                            break;
                        case Transform.Rotate270:

                            this.Data[(this.Width - x - 1) * this.Height + y] = data[y / vScale * width + x / hScale];
                            break;
                    }
                }
            }
            if (transform == Transform.Rotate90 || transform == Transform.Rotate270) {
                var temp = this.Width;
                this.Width = this.Height;
                this.Height = temp;
            }
        }
    }
}
