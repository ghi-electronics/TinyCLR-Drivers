using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace GHIElectronics.TinyCLR.Drivers.Epson.S1D13700 {
    public enum S1D13700CommandId : byte {
        SystemSet = 0x40,
        SleepIn = 0x53,
        DisplayOff = 0x58,
        DisplayOn = 0x59,
        Scroll = 0x44,
        CursorForm = 0x5D,
        CharGenRamAddress = 0x5C,
        CursorDirectionRight = 0x4C,
        CursorDirectionLeft = 0x4D,
        CursorDirectionUp = 0x4E,
        CursorDirectionDown = 0x4F,
        HorizontalScroll = 0x5A,
        Overlay = 0x5B,
        CursorWrite = 0x46,
        CursorRead = 0x47,
        MemoryWrite = 0x42,
        MemoryRead = 0x43
    }

    /// <summary>
    /// Driver for the Epson (Seiko Epson) S1D13700 graphics LCD controller, the successor of the SED1335.
    /// The controller is driven over its 8-bit parallel MPU bus (D0..D7 plus A0, /WR, /RD, /CS, /RES)
    /// using general purpose I/O. The default configuration matches a 320x240 monochrome panel
    /// (for example the Crystalfontz CFAG320240CX); other panels can be configured through the second
    /// constructor.
    /// </summary>
    public class S1D13700Controller {
        // Character cell size used for the internal text layer and the SYSTEM SET timing.
        private const int CharacterWidth = 8;
        private const int CharacterHeight = 8;

        private readonly GpioPin[] data; // D0..D7, index 0 == D0
        private readonly GpioPin a0;
        private readonly GpioPin wr;
        private readonly GpioPin rd;
        private readonly GpioPin cs;
        private readonly GpioPin reset;

        private readonly byte[] vram; // shadow of the graphics layer, 1bpp, MSB == left pixel
        private readonly int bytesPerLine;
        private readonly int textStart;
        private readonly int graphicStart;
        private readonly int textLength;
        private readonly byte horizontalTotalChars;

        public int Width { get; }
        public int Height { get; }

        /// <summary>Creates a controller for a 320x240 panel (CFAG320240CX timing).</summary>
        /// <param name="dataPins">Exactly eight output pins wired to D0..D7 (index 0 == D0).</param>
        /// <param name="a0">A0 line (command/data select).</param>
        /// <param name="wr">/WR write strobe.</param>
        /// <param name="rd">/RD read strobe. May be null; it is held inactive because the driver keeps a shadow buffer and never reads.</param>
        /// <param name="cs">/CS chip select.</param>
        /// <param name="reset">/RES reset line. May be null if tied to the board reset.</param>
        public S1D13700Controller(GpioPin[] dataPins, GpioPin a0, GpioPin wr, GpioPin rd, GpioPin cs, GpioPin reset)
            : this(dataPins, a0, wr, rd, cs, reset, 320, 240, 90) {
        }

        /// <summary>Creates a controller for a panel of an arbitrary supported size.</summary>
        /// <param name="width">Panel width in pixels. Must be a multiple of 8.</param>
        /// <param name="height">Panel height in pixels. Must be a multiple of the character height (8).</param>
        /// <param name="horizontalTotalChars">
        /// The SYSTEM SET TC/R register value: total bytes per line including the horizontal blanking period.
        /// This is panel specific (90 for the 320x240 CFAG320240CX); take it from your panel's datasheet.
        /// </param>
        public S1D13700Controller(GpioPin[] dataPins, GpioPin a0, GpioPin wr, GpioPin rd, GpioPin cs, GpioPin reset, int width, int height, int horizontalTotalChars) {
            if (dataPins == null || dataPins.Length != 8) throw new ArgumentException("dataPins must contain exactly eight pins (D0..D7).");
            if (a0 == null || wr == null || cs == null) throw new ArgumentNullException();
            if ((width % CharacterWidth) != 0) throw new ArgumentException("width must be a multiple of 8.");
            if ((height % CharacterHeight) != 0) throw new ArgumentException("height must be a multiple of 8.");

            this.Width = width;
            this.Height = height;
            this.bytesPerLine = width / CharacterWidth;
            this.horizontalTotalChars = (byte)horizontalTotalChars;

            // Memory map: text layer first (SAD1), then the graphics layer (SAD2), matching the reference design.
            this.textStart = 0;
            this.textLength = this.bytesPerLine * (height / CharacterHeight);
            this.graphicStart = this.textLength;
            this.vram = new byte[this.bytesPerLine * height];

            this.data = dataPins;
            this.a0 = a0;
            this.wr = wr;
            this.rd = rd;
            this.cs = cs;
            this.reset = reset;

            for (var i = 0; i < 8; i++)
                this.data[i].SetDriveMode(GpioPinDriveMode.Output);

            this.a0.SetDriveMode(GpioPinDriveMode.Output);
            this.wr.SetDriveMode(GpioPinDriveMode.Output);
            this.rd?.SetDriveMode(GpioPinDriveMode.Output);
            this.cs.SetDriveMode(GpioPinDriveMode.Output);
            this.reset?.SetDriveMode(GpioPinDriveMode.Output);

            // Idle the bus: strobes inactive (high).
            this.rd?.Write(GpioPinValue.High);
            this.wr.Write(GpioPinValue.High);
            this.cs.Write(GpioPinValue.High);
            this.a0.Write(GpioPinValue.High);

            this.Reset();
            this.Initialize();
            this.ClearText();
            this.Clear();
        }

        private void Reset() {
            if (this.reset == null)
                return;

            this.reset.Write(GpioPinValue.Low);
            Thread.Sleep(10);
            this.reset.Write(GpioPinValue.High);
            Thread.Sleep(10);
        }

        private void Initialize() {
            // SYSTEM SET: single panel, internal CG ROM, 8x8 character cell, two-frame AC drive.
            this.SendCommand(S1D13700CommandId.SystemSet);
            this.SendData(0x30);                                          // P1: IV=1, WS=0, single panel
            this.SendData((byte)(0x80 | (CharacterWidth - 1)));           // P2: WF=1 (two-frame), FX (char width-1)
            this.SendData(CharacterHeight - 1);                          // FY: character height-1
            this.SendData((byte)(this.bytesPerLine - 1));                // C/R: visible bytes per line-1
            this.SendData(this.horizontalTotalChars);                    // TC/R: total bytes per line incl. blanking
            this.SendData((byte)(this.Height - 1));                      // L/F: lines per frame-1
            this.SendData((byte)this.bytesPerLine);                     // AP low: address pitch
            this.SendData(0x00);                                         // AP high

            // SCROLL: layer 1 = text (SAD1), layer 2 = graphics (SAD2), both full height.
            this.SendCommand(S1D13700CommandId.Scroll);
            this.SendData((byte)(this.textStart & 0xFF));               // SAD1 low
            this.SendData((byte)((this.textStart >> 8) & 0xFF));        // SAD1 high
            this.SendData((byte)(this.Height - 1));                     // SL1
            this.SendData((byte)(this.graphicStart & 0xFF));           // SAD2 low
            this.SendData((byte)((this.graphicStart >> 8) & 0xFF));    // SAD2 high
            this.SendData((byte)(this.Height - 1));                     // SL2
            this.SendData(0x00);                                        // SAD3 low
            this.SendData(0x00);                                        // SAD3 high
            this.SendData(0x00);                                        // SAD4 low
            this.SendData(0x00);                                        // SAD4 high

            // CSRFORM: block cursor, 8x8.
            this.SendCommand(S1D13700CommandId.CursorForm);
            this.SendData(CharacterWidth - 1);                          // CRX
            this.SendData(CharacterHeight - 1);                        // CRY

            // CGRAM ADR: character generator RAM base (unused by the internal ROM, set to reference default).
            this.SendCommand(S1D13700CommandId.CharGenRamAddress);
            this.SendData(0x00);
            this.SendData(0x70);

            // Cursor auto-increments to the right after each memory access.
            this.SendCommand(S1D13700CommandId.CursorDirectionRight);

            // No horizontal pixel scroll.
            this.SendCommand(S1D13700CommandId.HorizontalScroll);
            this.SendData(0x00);

            // OVLAY: two layers, OR-combined (text over graphics).
            this.SendCommand(S1D13700CommandId.Overlay);
            this.SendData(0x01);

            // DISP ON: display on, text and graphics layers visible, cursor off.
            this.SendCommand(S1D13700CommandId.DisplayOn);
            this.SendData(0x16);
        }

        /// <summary>Clears the graphics layer (the shadow buffer and the panel).</summary>
        public void Clear() {
            Array.Clear(this.vram, 0, this.vram.Length);
            this.Flush();
        }

        /// <summary>Fills the text layer with spaces so it does not OR garbage over the graphics layer.</summary>
        public void ClearText() {
            this.SetCursorAddress(this.textStart);
            this.SendCommand(S1D13700CommandId.MemoryWrite);

            this.a0.Write(GpioPinValue.Low);
            this.cs.Write(GpioPinValue.Low);
            for (var i = 0; i < this.textLength; i++)
                this.WriteDataByte(0x20);
            this.cs.Write(GpioPinValue.High);
        }

        /// <summary>Writes text at a character cell on the internal text layer using the on-chip font.</summary>
        public void DrawText(string text, int column, int row) {
            if (text == null) throw new ArgumentNullException();

            this.SetCursorAddress(this.textStart + row * this.bytesPerLine + column);
            this.SendCommand(S1D13700CommandId.MemoryWrite);

            this.a0.Write(GpioPinValue.Low);
            this.cs.Write(GpioPinValue.Low);
            for (var i = 0; i < text.Length; i++)
                this.WriteDataByte((byte)text[i]);
            this.cs.Write(GpioPinValue.High);
        }

        /// <summary>Sets or clears a single pixel and immediately updates the panel.</summary>
        public void SetPixel(int x, int y, bool on) {
            if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
                return;

            var index = y * this.bytesPerLine + (x / 8);
            var mask = (byte)(0x80 >> (x % 8)); // MSB == leftmost pixel

            if (on)
                this.vram[index] |= mask;
            else
                this.vram[index] &= (byte)~mask;

            this.SetCursorAddress(this.graphicStart + index);
            this.SendCommand(S1D13700CommandId.MemoryWrite);
            this.SendData(this.vram[index]);
        }

        /// <summary>Copies a full 1bpp frame (MSB == left pixel, width/8 bytes per row) and pushes it to the panel.</summary>
        public void DrawBuffer(byte[] buffer) => this.DrawBufferNative(buffer, 0, buffer.Length);

        public void DrawBufferNative(byte[] buffer) => this.DrawBufferNative(buffer, 0, buffer.Length);

        public void DrawBufferNative(byte[] buffer, int offset, int count) {
            Array.Copy(buffer, offset, this.vram, 0, count);
            this.Flush();
        }

        /// <summary>Writes the entire shadow buffer to the graphics layer.</summary>
        public void Flush() {
            this.SetCursorAddress(this.graphicStart);
            this.SendCommand(S1D13700CommandId.MemoryWrite);

            // Burst write: hold A0 (data) and /CS low, strobe /WR per byte, cursor auto-increments.
            this.a0.Write(GpioPinValue.Low);
            this.cs.Write(GpioPinValue.Low);
            for (var i = 0; i < this.vram.Length; i++)
                this.WriteDataByte(this.vram[i]);
            this.cs.Write(GpioPinValue.High);
        }

        public void Dispose() {
            for (var i = 0; i < 8; i++)
                this.data[i].Dispose();

            this.a0.Dispose();
            this.wr.Dispose();
            this.rd?.Dispose();
            this.cs.Dispose();
            this.reset?.Dispose();
        }

        private void SetCursorAddress(int address) {
            this.SendCommand(S1D13700CommandId.CursorWrite);
            this.SendData((byte)(address & 0xFF));
            this.SendData((byte)((address >> 8) & 0xFF));
        }

        private void SendCommand(S1D13700CommandId command) {
            this.WriteBus((byte)command);
            this.a0.Write(GpioPinValue.High); // A0 high == command
            this.cs.Write(GpioPinValue.Low);
            this.wr.Write(GpioPinValue.Low);
            this.wr.Write(GpioPinValue.High); // latch on /WR rising edge
            this.cs.Write(GpioPinValue.High);
        }

        private void SendData(byte value) {
            this.WriteBus(value);
            this.a0.Write(GpioPinValue.Low); // A0 low == data
            this.cs.Write(GpioPinValue.Low);
            this.wr.Write(GpioPinValue.Low);
            this.wr.Write(GpioPinValue.High);
            this.cs.Write(GpioPinValue.High);
        }

        private void SendData(int value) => this.SendData((byte)value);

        // Assumes A0 (data) and /CS are already held low by the caller (burst path).
        private void WriteDataByte(byte value) {
            this.WriteBus(value);
            this.wr.Write(GpioPinValue.Low);
            this.wr.Write(GpioPinValue.High);
        }

        private void WriteBus(byte value) {
            this.data[0].Write((value & 0x01) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[1].Write((value & 0x02) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[2].Write((value & 0x04) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[3].Write((value & 0x08) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[4].Write((value & 0x10) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[5].Write((value & 0x20) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[6].Write((value & 0x40) != 0 ? GpioPinValue.High : GpioPinValue.Low);
            this.data[7].Write((value & 0x80) != 0 ? GpioPinValue.High : GpioPinValue.Low);
        }
    }
}
