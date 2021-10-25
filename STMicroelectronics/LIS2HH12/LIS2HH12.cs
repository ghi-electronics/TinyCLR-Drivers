using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.STMicroelectronics.LIS2HH12 {
    public sealed class LIS2HH12Controller {
        const int LIS2HH12_WHO_AM_I = 0x0F;

        const int LIS2HH12_CTRL1 = 0x20;
        const int LIS2HH12_CTRL5 = 0x24;

        const int LIS2HH12_OUT_X_L = 0x28;
        const int LIS2HH12_OUT_Y_L = 0x2A;
        const int LIS2HH12_OUT_Z_L = 0x2C;

        const int LIS2HH12_XL_ODR_100Hz = 0x03;

        const int LIS2HH12_ID = 0x41;

        private readonly I2cDevice i2cDevice;

        public double X => this.GetX();
        public double Y => this.GetY();
        public double Z => this.GetZ();

        public LIS2HH12Controller(I2cController i2cController) {

            var setting = new I2cConnectionSettings(0x1D) {
                BusSpeed = 100000,
                AddressFormat = I2cAddressFormat.SevenBit,
            };

            this.i2cDevice = i2cController.GetDevice(setting);

            var id = this.ReadFromRegister(LIS2HH12_WHO_AM_I, 1);

            if (id[0] != LIS2HH12_ID) {
                throw new InvalidOperationException("Wrong Id detected!");
            }

            this.Reset();
            this.SetDataRate100Hz();
        }

        private void Reset() {
            var data = this.ReadFromRegister(LIS2HH12_CTRL5, 1);

            data[0] |= (byte)(1 << 6);

            this.WriteToRegister(LIS2HH12_CTRL5, data);

            var d = data[0] & (1 << 6);

            while (d > 0) {
                data = this.ReadFromRegister(LIS2HH12_CTRL5, 1);

                d = data[0] & (1 << 6);
            }
        }

        private void SetDataRate100Hz() {

            var data = this.ReadFromRegister(LIS2HH12_CTRL1, 1);

            data[0] |= (byte)(LIS2HH12_XL_ODR_100Hz << 4);

            this.WriteToRegister(LIS2HH12_CTRL1, data);
        }

        private void WriteToRegister(byte reg, byte[] value) {
            var count = value.Length + 1;

            var write = new byte[count];

            write[0] = reg;

            Array.Copy(value, 0, write, 1, value.Length);

            this.i2cDevice.Write(write, 0, write.Length);
        }

        private byte[] ReadFromRegister(byte reg, int count) {
            var writeData = new byte[1] { reg };
            var readData = new byte[count];

            this.i2cDevice.WriteRead(writeData, readData);

            return readData;
        }

        static double CovertFsToMg(int value) => value * 0.061f;

        private double GetX() {
            var data = this.ReadFromRegister(LIS2HH12_OUT_X_L, 2);
            var raw = (data[0] << 0) | (data[1] << 8);

            if (raw > 32767)
                raw = raw - 65536;

            return CovertFsToMg(raw);
        }

        private double GetY() {
            var data = this.ReadFromRegister(LIS2HH12_OUT_Y_L, 2);

            var raw = (data[0] << 0) | (data[1] << 8);

            if (raw > 32767)
                raw = raw - 65536;

            return CovertFsToMg(raw);
        }

        private double GetZ() {
            var data = this.ReadFromRegister(LIS2HH12_OUT_Z_L, 2);
            var raw = (data[0] << 0) | (data[1] << 8);

            if (raw > 32767)
                raw = raw - 65536;

            return CovertFsToMg(raw);
        }
    }
}
