using System;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.FocalTech.FT5xx6 {
    public class TouchEventArgs : EventArgs {
        public int X { get; }
        public int Y { get; }

        public TouchEventArgs(int x, int y) {
            this.X = x;
            this.Y = y;
        }
    }
    public enum Gesture {
        MoveUp = 0x1C,
        MoveDown = 0x14,
        MoveLeft = 0x10,
        MoveRight = 0x18,
        ZoomIn = 0x48,
        ZoomOut = 0x49,
    }

    public delegate void TouchEventHandler(FT5xx6Controller sender, TouchEventArgs e);
    public delegate void GestureHandler(FT5xx6Controller sender, Gesture gesture);

    public class FT5xx6Controller : IDisposable {
        private readonly byte[] addressBuffer = new byte[1];
        private readonly byte[] read7 = new byte[7];
        private readonly I2cDevice i2c;
        private readonly GpioPin interrupt;

        public event TouchEventHandler TouchDown;
        public event TouchEventHandler TouchUp;
        public event TouchEventHandler TouchMove;
        public event GestureHandler OnGesture;

        public static I2cConnectionSettings GetConnectionSettings() => new I2cConnectionSettings(0x38) {
            BusSpeed = I2cBusSpeed.FastMode,
            AddressFormat = I2cAddressFormat.SevenBit,
        };

        public FT5xx6Controller(I2cDevice i2c, GpioPin interrupt) {
            this.i2c = i2c;

            this.interrupt = interrupt;
            this.interrupt.SetDriveMode(GpioPinDriveMode.Input);
            this.interrupt.DebounceTimeout = TimeSpan.FromMilliseconds(1);
            this.interrupt.ValueChangedEdge = GpioPinEdge.FallingEdge;
            this.interrupt.ValueChanged += this.OnInterrupt;
        }

        public void Dispose() {
            this.i2c.Dispose();
            this.interrupt.Dispose();
        }

        private void OnInterrupt(GpioPin sender, GpioPinValueChangedEventArgs e) {
            this.addressBuffer[0] = (byte)0;

            this.i2c.WriteRead(this.addressBuffer, read7);

            if (read7[1] != 0) this.OnGesture?.Invoke(this, (Gesture)read7[1]);

            var x = ((read7[3] & 0x0F) << 8) | read7[4];
            var y = ((read7[5] & 0x0F) << 8) | read7[6];
                var flag = (read7[3] & 0xC0) >> 6;
            (flag == 0 ? this.TouchDown : flag == 1 ? this.TouchUp : flag == 2 ? this.TouchMove : null)?.Invoke(this, new TouchEventArgs(x, y));
        }

        private byte[] ReadData(int address, byte[] buffer) {
            this.addressBuffer[0] = (byte)address;

            this.i2c.WriteRead(this.addressBuffer, buffer);

            return buffer;
        }
    }
}
