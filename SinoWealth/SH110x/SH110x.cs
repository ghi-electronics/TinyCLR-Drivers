using System;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.SinoWealth.SH110x {
    public enum SH110xChip {
        /// <summary>132x64 RAM behind 128x64 glass. Needs a column offset (usually 2). No horizontal addressing mode.</summary>
        SH1106 = 0,
        /// <summary>128x128 RAM (16 pages). Used for 128x64 panels and for rotated 64x128 panels.</summary>
        SH1107 = 1
    }

    /// <summary>
    /// Driver for SH1106 / SH1107 1bpp OLED panels over I2C.
    ///
    /// API compatible with SSD1306Controller (GetConnectionSettings / DrawBufferNative /
    /// SetColorFormat / Dispose) so an existing SSD1306 sketch only needs the type name swapped.
    ///
    /// Differences from SSD1306 that this driver handles:
    ///  - SH110x has no horizontal addressing mode and no 0x21/0x22 column/page range commands,
    ///    so a frame is pushed one page at a time (set page, set column low/high, write bytes).
    ///  - SH1106 has 132 columns of RAM behind 128 columns of glass, so the visible area normally
    ///    starts at column 2. A wrong offset shows up as a sideways shift with a junk stripe at
    ///    one edge. Tweak it at runtime with the ColumnOffset property if 2 is not right.
    ///  - The charge pump command is 0xAD 0x8B, not the SSD1306 0x8D 0x14. Sending the SSD1306
    ///    version leaves some modules dark.
    ///
    /// Input buffer layout is the same as SSD1306 and BasicGraphics OneBpp:
    ///   index = (y / 8) * Width + x, bit = 1 shifted left by (y % 8).
    /// </summary>
    public class SH110xController {
        // Command set. Shared with SSD1306 except where noted.
        private const byte CMD_SET_LOW_COLUMN = 0x00;
        private const byte CMD_SET_HIGH_COLUMN = 0x10;
        private const byte CMD_SET_MEMORY_MODE = 0x20;      // SH1107: 0x20 page mode, 0x21 vertical mode
        private const byte CMD_SET_CONTRAST = 0x81;
        private const byte CMD_SET_SEG_REMAP = 0xA0;        // OR 1 for reversed
        private const byte CMD_SET_MULTIPLEX = 0xA8;
        private const byte CMD_DC_DC = 0xAD;                // SH110x charge pump. SSD1306 uses 0x8D
        private const byte CMD_DC_DC_ON = 0x8B;
        private const byte CMD_DISPLAY_OFF = 0xAE;
        private const byte CMD_DISPLAY_ON = 0xAF;
        private const byte CMD_SET_PAGE = 0xB0;
        private const byte CMD_SET_COM_SCAN_INC = 0xC0;
        private const byte CMD_SET_COM_SCAN_DEC = 0xC8;
        private const byte CMD_SET_DISPLAY_OFFSET = 0xD3;
        private const byte CMD_SET_CLOCK_DIV = 0xD5;
        private const byte CMD_SET_PRECHARGE = 0xD9;
        private const byte CMD_SET_COM_PINS = 0xDA;
        private const byte CMD_SET_VCOM_DETECT = 0xDB;
        private const byte CMD_SET_START_LINE_1107 = 0xDC;  // SH1107 start line is 2 bytes. SSD1306/SH1106 use 0x40 OR line

        private readonly I2cDevice i2c;
        private readonly SH110xChip chip;
        private readonly int width;
        private readonly int height;
        private readonly bool rotate90;

        private readonly byte[] vram;           // caller space frame, (width * height) / 8
        private readonly byte[] shadow;         // what was last pushed, for skip unchanged pages
        private readonly byte[] pageBuffer;     // 0x40 control byte plus one controller page
        private readonly byte[] commandBuffer = new byte[2];

        private readonly int pageCount;         // controller pages pushed per frame
        private readonly int pageWidth;         // controller columns per page

        private int columnOffset;
        private bool forceFullFlush = true;

        public int Width => this.width;
        public int Height => this.height;
        public SH110xChip Chip => this.chip;

        /// <summary>
        /// First visible RAM column. SH1106 on 128 wide glass is almost always 2, a few clones are 0.
        /// If the image is shifted sideways with junk at one edge, this is the knob to turn.
        /// </summary>
        public int ColumnOffset {
            get => this.columnOffset;
            set {
                this.columnOffset = value;
                this.forceFullFlush = true;
            }
        }

        /// <summary>When true (default) pages whose content did not change are not re-sent over I2C.</summary>
        public bool SkipUnchangedPages { get; set; } = true;

        /// <summary>0x3C is the usual address, 0x3D when SA0 is pulled high.</summary>
        public static I2cConnectionSettings GetConnectionSettings() => GetConnectionSettings(0x3C);

        public static I2cConnectionSettings GetConnectionSettings(int slaveAddress) => new I2cConnectionSettings(slaveAddress) {
            AddressFormat = I2cAddressFormat.SevenBit,
            BusSpeed = 400000,
        };

        /// <summary>SH1106 128x64 with the usual column offset of 2.</summary>
        public SH110xController(I2cDevice i2c) : this(i2c, SH110xChip.SH1106) {
        }

        /// <summary>128x64 panel, default column offset for the chip: 2 for SH1106, 0 for SH1107.</summary>
        public SH110xController(I2cDevice i2c, SH110xChip chip) : this(i2c, chip, 128, 64, chip == SH110xChip.SH1106 ? 2 : 0, false) {
        }

        /// <param name="rotate90">
        /// SH1107 only. Set this for panels whose glass is physically 64 wide by 128 tall but that you
        /// want to drive as 128x64 landscape (the Adafruit style 1.12 inch and FeatherWing modules).
        /// The frame is transposed in software, which costs real CPU time, so leave it false unless
        /// the picture comes out sideways.
        /// </param>
        public SH110xController(I2cDevice i2c, SH110xChip chip, int width, int height, int columnOffset, bool rotate90) {
            if (i2c == null) throw new ArgumentNullException();
            if (width <= 0 || height <= 0 || (width % 8) != 0 || (height % 8) != 0) throw new ArgumentException();
            if (rotate90 && chip != SH110xChip.SH1107) throw new ArgumentException();

            this.i2c = i2c;
            this.chip = chip;
            this.width = width;
            this.height = height;
            this.columnOffset = columnOffset;
            this.rotate90 = rotate90;

            // Not rotated: a controller page spans the screen width and pages stack down the height.
            // Rotated: the frame is transposed, so controller columns run down the screen height.
            this.pageWidth = rotate90 ? height : width;
            this.pageCount = rotate90 ? width / 8 : height / 8;

            this.vram = new byte[width * height / 8];
            this.shadow = new byte[this.pageCount * this.pageWidth];
            this.pageBuffer = new byte[this.pageWidth + 1];
            this.pageBuffer[0] = 0x40; // data stream control byte

            this.Initialize();
        }

        private void Initialize() {
            if (this.chip == SH110xChip.SH1106)
                this.InitializeSH1106();
            else
                this.InitializeSH1107();

            this.Clear();

            // GDDRAM powers up random, so only light the panel once it has been cleared.
            this.SendCommand(CMD_DISPLAY_ON);
        }

        private void InitializeSH1106() {
            this.SendCommand(CMD_DISPLAY_OFF);
            this.SendCommand(CMD_SET_CLOCK_DIV); this.SendCommand(0x80);
            this.SendCommand(CMD_SET_MULTIPLEX); this.SendCommand((byte)(this.height - 1));
            this.SendCommand(CMD_SET_DISPLAY_OFFSET); this.SendCommand(0x00);
            this.SendCommand(0x40);                                             // start line 0
            this.SendCommand(CMD_DC_DC); this.SendCommand(CMD_DC_DC_ON);        // internal charge pump on
            this.SendCommand(0x32);                                             // pump output 8.0V, range 0x30 to 0x33
            this.SendCommand((byte)(CMD_SET_SEG_REMAP | 0x01));                 // column 127 maps to SEG0
            this.SendCommand(CMD_SET_COM_SCAN_DEC);                             // scan COM[N-1] down to COM0
            this.SendCommand(CMD_SET_COM_PINS); this.SendCommand((byte)(this.height > 32 ? 0x12 : 0x02));
            this.SendCommand(CMD_SET_CONTRAST); this.SendCommand(0x80);
            this.SendCommand(CMD_SET_PRECHARGE); this.SendCommand(0x22);
            this.SendCommand(CMD_SET_VCOM_DETECT); this.SendCommand(0x35);
            this.SendCommand(0xA4);                                             // resume from RAM
            this.SendCommand(0xA6);                                             // normal, not inverted
        }

        private void InitializeSH1107() {
            this.SendCommand(CMD_DISPLAY_OFF);
            this.SendCommand(CMD_SET_CLOCK_DIV); this.SendCommand(0x51);
            this.SendCommand(CMD_SET_MEMORY_MODE);                              // page addressing mode
            this.SendCommand(CMD_SET_CONTRAST); this.SendCommand(0x4F);
            this.SendCommand(CMD_DC_DC); this.SendCommand(CMD_DC_DC_ON);
            this.SendCommand(CMD_SET_SEG_REMAP);                                // 0xA0 normal, 0xA1 mirrored
            this.SendCommand(CMD_SET_COM_SCAN_INC);                             // 0xC0 normal, 0xC8 flipped
            this.SendCommand(CMD_SET_START_LINE_1107); this.SendCommand(0x00);
            this.SendCommand(CMD_SET_DISPLAY_OFFSET); this.SendCommand(0x00);
            this.SendCommand(CMD_SET_MULTIPLEX); this.SendCommand((byte)((this.rotate90 ? this.width : this.height) - 1));
            this.SendCommand(CMD_SET_PRECHARGE); this.SendCommand(0x22);
            this.SendCommand(CMD_SET_VCOM_DETECT); this.SendCommand(0x35);
            this.SendCommand(0xA4);
            this.SendCommand(0xA6);
        }

        /// <summary>Raw command escape hatch, useful while feeling out an unknown module.</summary>
        public void SendCommand(byte command) {
            this.commandBuffer[0] = 0x00; // command stream control byte
            this.commandBuffer[1] = command;

            this.i2c.Write(this.commandBuffer);
        }

        public void SendCommands(byte[] commands) {
            for (var i = 0; i < commands.Length; i++)
                this.SendCommand(commands[i]);
        }

        public void SetColorFormat(bool invert) => this.SendCommand((byte)(invert ? 0xA7 : 0xA6));

        public void SetContrast(byte level) {
            this.SendCommand(CMD_SET_CONTRAST);
            this.SendCommand(level);
        }

        /// <summary>Turns the picture 180 degrees. Both commands have to agree or the image comes out mirrored.</summary>
        public void SetUpsideDown(bool upsideDown) {
            this.SendCommand((byte)(upsideDown ? CMD_SET_SEG_REMAP : (CMD_SET_SEG_REMAP | 0x01)));
            this.SendCommand(upsideDown ? CMD_SET_COM_SCAN_INC : CMD_SET_COM_SCAN_DEC);
        }

        public void TurnOn() => this.SendCommand(CMD_DISPLAY_ON);

        public void TurnOff() => this.SendCommand(CMD_DISPLAY_OFF);

        public void Clear() {
            Array.Clear(this.vram, 0, this.vram.Length);

            // Wipe every RAM column, not just the visible window. SH1106 has 132 columns behind
            // 128 of glass, so columns outside the current ColumnOffset window would otherwise keep
            // showing power-up garbage as a stripe at one edge.
            this.ClearControllerRam();

            Array.Clear(this.shadow, 0, this.shadow.Length);

            this.forceFullFlush = false; // controller RAM and shadow are both known zero now
        }

        private void ClearControllerRam() {
            var ramWidth = this.chip == SH110xChip.SH1106 ? 132 : 128;
            var ramPages = this.chip == SH110xChip.SH1106 ? 8 : 16;
            var zeros = new byte[ramWidth + 1];

            zeros[0] = 0x40; // data stream control byte

            for (var page = 0; page < ramPages; page++) {
                this.SendCommand((byte)(CMD_SET_PAGE | page));
                this.SendCommand(CMD_SET_LOW_COLUMN);
                this.SendCommand(CMD_SET_HIGH_COLUMN);

                this.i2c.Write(zeros);
            }
        }

        public void DrawBufferNative(byte[] buffer) => this.DrawBufferNative(buffer, 0, buffer.Length);

        public void DrawBufferNative(byte[] buffer, int offset, int count) {
            if (count > this.vram.Length) count = this.vram.Length;

            Array.Copy(buffer, offset, this.vram, 0, count);

            this.Flush();
        }

        /// <summary>Pushes vram to the panel one page per transaction. SH110x has no horizontal addressing.</summary>
        private void Flush() {
            var full = this.forceFullFlush;

            this.forceFullFlush = false;

            for (var page = 0; page < this.pageCount; page++) {
                var shadowStart = page * this.pageWidth;

                if (this.rotate90)
                    this.BuildRotatedPage(page);
                else
                    Array.Copy(this.vram, shadowStart, this.pageBuffer, 1, this.pageWidth);

                if (!full && this.SkipUnchangedPages && this.PageUnchanged(shadowStart))
                    continue;

                Array.Copy(this.pageBuffer, 1, this.shadow, shadowStart, this.pageWidth);

                this.SendCommand((byte)(CMD_SET_PAGE | page));
                this.SendCommand((byte)(CMD_SET_LOW_COLUMN | (this.columnOffset & 0x0F)));
                this.SendCommand((byte)(CMD_SET_HIGH_COLUMN | ((this.columnOffset >> 4) & 0x0F)));

                this.i2c.Write(this.pageBuffer);
            }
        }

        private bool PageUnchanged(int shadowStart) {
            for (var i = 0; i < this.pageWidth; i++)
                if (this.pageBuffer[i + 1] != this.shadow[shadowStart + i])
                    return false;

            return true;
        }

        /// <summary>
        /// Transposes a landscape frame into the portrait layout an SH1107 64x128 panel expects.
        /// Controller page p covers screen columns x = p * 8 through p * 8 + 7, and controller
        /// column c is screen row y.
        /// </summary>
        private void BuildRotatedPage(int page) {
            var baseX = page * 8;

            for (var c = 0; c < this.pageWidth; c++) {
                var rowStart = (c >> 3) * this.width;
                var mask = (byte)(1 << (c & 7));
                byte value = 0;

                for (var bit = 0; bit < 8; bit++) {
                    if ((this.vram[rowStart + baseX + bit] & mask) != 0)
                        value |= (byte)(1 << bit);
                }

                this.pageBuffer[c + 1] = value;
            }
        }

        public void Dispose() => this.i2c.Dispose();
    }
}
