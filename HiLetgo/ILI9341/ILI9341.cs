using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.HiLetgo.ILI9341 {
    public class ILI9341 {
        public enum ILI9341CommandId : byte {
            SWRESET = 0x01,
            SLPOUT = 0x11,
            INVOFF = 0x20,
            INVON = 0x21,
            GAMMASET = 0x26,
            DISPOFF = 0x28,
            DISPON = 0x29,
            CASET = 0x2A,
            PASET = 0x2B,
            RAMWR = 0x2C,
            MADCTL = 0x36,
            PIXFMT = 0x3A,
            FRMCTR1 = 0xB1,
            DFUNCTR = 0xB6,
            EMSET = 0xB7,
            MADCTL_MY = 0x80,
            MADCTL_MX = 0x40,
            MADCTL_MV = 0x20,
            MADCTL_BGR = 0x08,
            MADCTL_RGB = 0x00
        }

        public class ILI9341Controller {
            private readonly byte[] buffer1 = new byte[1];

            private readonly SpiDevice spi;
            private readonly GpioPin control;
            private readonly GpioPin reset;

            private bool rowColumnSwapped;

            public int Width {
                get; private set;
            }
            public int Height {
                get; private set;
            }

            private int bpp = 16;

            public int MaxWidth => this.rowColumnSwapped ? 320 : 240;
            public int MaxHeight => this.rowColumnSwapped ? 240 : 320;

            public static SpiConnectionSettings GetConnectionSettings(SpiChipSelectType chipSelectType, GpioPin chipSelectLine) => new SpiConnectionSettings {
                Mode = SpiMode.Mode0,
                ClockFrequency = 12_000_000,
                ChipSelectType = chipSelectType,
                ChipSelectLine = chipSelectLine
            };

            public ILI9341Controller(SpiDevice spi, GpioPin control) : this(spi, control, null) {

            }

            public ILI9341Controller(SpiDevice spi, GpioPin control, GpioPin reset) {
                this.spi = spi;

                this.control = control;
                this.control.SetDriveMode(GpioPinDriveMode.Output);

                this.reset = reset;
                this.reset?.SetDriveMode(GpioPinDriveMode.Output);


                this.Reset();
                this.Initialize();
                this.SetDataAccessControl(false, false, false, true);
                this.SetDrawWindow(0, 0, this.MaxWidth, this.MaxHeight);

                this.Enable();
            }

            private void Reset() {
                this.reset?.Write(GpioPinValue.Low);
                Thread.Sleep(50);

                this.reset?.Write(GpioPinValue.High);
                Thread.Sleep(200);
            }

            private void Initialize() {
                this.SendCommand(ILI9341CommandId.SWRESET);
                Thread.Sleep(10);

                this.SendCommand(ILI9341CommandId.DISPOFF);

                this.SendCommand(ILI9341CommandId.MADCTL);

                this.SendData(0x08 | 0x40);

                this.SendCommand(ILI9341CommandId.PIXFMT);
                this.SendData(0x55);

                this.SendCommand(ILI9341CommandId.FRMCTR1);
                this.SendData(0x00);
                this.SendData(0x1B);

                this.SendCommand(ILI9341CommandId.GAMMASET);
                this.SendData(0x01);

                this.SendCommand(ILI9341CommandId.CASET);
                this.SendData(0x00);
                this.SendData(0x00);
                this.SendData(0x00);
                this.SendData(0xEF);

                this.SendCommand(ILI9341CommandId.PASET);
                this.SendData(0x00);
                this.SendData(0x00);
                this.SendData(0x01);
                this.SendData(0x3F);

                this.SendCommand(ILI9341CommandId.EMSET);
                this.SendData(0x07);

                this.SendCommand(ILI9341CommandId.DFUNCTR);
                this.SendData(0x0A);
                this.SendData(0x82);
                this.SendData(0x27);
                this.SendData(0x00);

                this.SendCommand(ILI9341CommandId.SLPOUT);
                Thread.Sleep(120);

                this.SendCommand(ILI9341CommandId.DISPON);
                Thread.Sleep(100);
            }

            public void Dispose() {
                this.spi.Dispose();
                this.control.Dispose();
                this.reset?.Dispose();
            }

            public void Enable() => this.SendCommand(ILI9341CommandId.DISPON);
            public void Disable() => this.SendCommand(ILI9341CommandId.DISPOFF);

            private void SendCommand(ILI9341CommandId command) {
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


            public void SetDrawWindow(int x, int y, int width, int height) {
                var x_end = x + width - 1;
                var y_end = y + height - 1;

                this.SendCommand(ILI9341CommandId.CASET);
                this.SendData((byte)(x >> 8));
                this.SendData((byte)x);
                this.SendData((byte)(x_end >> 8));
                this.SendData((byte)x_end);

                this.SendCommand(ILI9341CommandId.PASET);
                this.SendData((byte)(y >> 8));
                this.SendData((byte)y);
                this.SendData((byte)(y_end >> 8));
                this.SendData((byte)y_end);

                this.SendCommand(ILI9341CommandId.RAMWR);

                this.Width = width;
                this.Height = height;
            }

            public void SetDataAccessControl(bool swapRowColumn, bool invertRow, bool invertColumn, bool useBgrPanel) {
                var val = default(byte);

                if (useBgrPanel) val |= 0b0000_1000;
                if (swapRowColumn) val |= 0b0010_0000;
                if (invertColumn) val |= 0b0100_0000;
                if (invertRow) val |= 0b1000_0000;

                this.SendCommand(ILI9341CommandId.MADCTL);
                this.SendData(val);

                this.rowColumnSwapped = swapRowColumn;
            }

            private void SendDrawCommand() {
                this.SendCommand(ILI9341CommandId.RAMWR);
                this.control.Write(GpioPinValue.High);
            }

            public void DrawBuffer(byte[] buffer) {
                this.SendDrawCommand();

                BitConverter.SwapEndianness(buffer, 2);

                this.spi.Write(buffer, 0, this.Height * this.Width * this.bpp / 8);

                BitConverter.SwapEndianness(buffer, 2);
            }

            public void DrawBuffer(byte[] buffer, int x, int y, int width, int height) {
                this.SetDrawWindow(x, y, width, height);

                this.DrawBuffer(buffer, x, y, width, height, this.MaxWidth, 1, 1);
            }

            public void DrawBuffer(byte[] buffer, int x, int y, int width, int height, int originalWidth, int columnMultiplier, int rowMultiplier) {
                this.SendDrawCommand();

                BitConverter.SwapEndianness(buffer, 2);

                this.spi.Write(buffer, x, y, width, height, originalWidth, columnMultiplier, rowMultiplier);

                BitConverter.SwapEndianness(buffer, 2);
            }

        }
    }
}
