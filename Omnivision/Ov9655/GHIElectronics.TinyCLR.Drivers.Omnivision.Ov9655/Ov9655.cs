using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Dcmi;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.Omnivision.Ov9655
{
    public class Ov9655
    {
        private const byte I2C_ADDRESS = 0x30;

        private I2cDevice i2cDevice;
        private DcmiController dcmiController;
        public Ov9655(I2cController i2cController) {

            var settings = new I2cConnectionSettings(I2C_ADDRESS) {

                BusSpeed = I2cBusSpeed.StandardMode,
                AddressFormat = I2cAddressFormat.SevenBit,

            };

            this.i2cDevice = i2cController.GetDevice(settings);            

            this.dcmiController = GHIElectronics.TinyCLR.Devices.Dcmi.DcmiController.GetDefault();
            this.dcmiController.SetActiveSettings(GHIElectronics.TinyCLR.Devices.Dcmi.CaptureRate.AllFrame, false, true, true, GHIElectronics.TinyCLR.Devices.Dcmi.SynchronizationMode.Hardware, GHIElectronics.TinyCLR.Devices.Dcmi.ExtendedDataMode.Extended8bit);
            this.dcmiController.Enable();

            this.Reset();

        }

        public string ReadId() {

            this.WriteRegister(0xff, 1);

            var id1 = this.ReadRegister(0x0A);
            var id2 = this.ReadRegister(0x0B);

            var id = (id1 << 8) | id2;

            return id.ToString("x");

        }

        public void Capture(byte[] data) => this.dcmiController.Capture(data);

        public void SetResolution(Resolution size) {
            switch (size) {
                case Resolution.Vga:
                    this.SetVga();
                    break;

                case Resolution.Qvga:
                    //Todo
                    break;

                case Resolution.Qqvga:
                    //Todo
                    break;
            }
        }
        
        private void SetVga() {
            this.WriteRegister( 0x00, 0x00 );
            this.WriteRegister( 0x01, 0x80 );
            this.WriteRegister( 0x02, 0x80 );
            this.WriteRegister( 0xb5, 0x00 );
            this.WriteRegister( 0x35, 0x00 );
            this.WriteRegister( 0xa8, 0xc1 );
            this.WriteRegister( 0x3a, 0xcc );
            this.WriteRegister( 0x3d, 0x99 );
            this.WriteRegister( 0x77, 0x02 );
            this.WriteRegister( 0x13, 0xe7 );
            this.WriteRegister( 0x26, 0x72 );
            this.WriteRegister( 0x27, 0x08 );
            this.WriteRegister( 0x28, 0x08 );
            this.WriteRegister( 0x2c, 0x08 );
            this.WriteRegister( 0xab, 0x04 );
            this.WriteRegister( 0x6e, 0x00 );
            this.WriteRegister( 0x6d, 0x55 );
            this.WriteRegister( 0x00, 0x11 );
            this.WriteRegister( 0x10, 0x7b );
            this.WriteRegister( 0xbb, 0xae );
            this.WriteRegister( 0x11, 0x03 );
            this.WriteRegister( 0x72, 0x00 );
            this.WriteRegister( 0x3e, 0x0c );
            this.WriteRegister( 0x74, 0x3a );
            this.WriteRegister( 0x76, 0x01 );
            this.WriteRegister( 0x75, 0x35 );
            this.WriteRegister( 0x73, 0x00 );
            this.WriteRegister( 0xc7, 0x80 );
            this.WriteRegister( 0x62, 0x00 );
            this.WriteRegister( 0x63, 0x00 );
            this.WriteRegister( 0x64, 0x02 );
            this.WriteRegister( 0x65, 0x20 );
            this.WriteRegister( 0x66, 0x01 );
            this.WriteRegister( 0xc3, 0x4e );
            this.WriteRegister( 0x33, 0x00 );
            this.WriteRegister( 0xa4, 0x50 );
            this.WriteRegister( 0xaa, 0x92 );
            this.WriteRegister( 0xc2, 0x01 );
            this.WriteRegister( 0xc1, 0xc8 );
            this.WriteRegister( 0x1e, 0x04 );
            this.WriteRegister( 0xa9, 0xef );
            this.WriteRegister( 0x0e, 0x61 );
            this.WriteRegister( 0x39, 0x57 );
            this.WriteRegister( 0x0f, 0x48 );
            this.WriteRegister( 0x24, 0x3c );
            this.WriteRegister( 0x25, 0x36 );
            this.WriteRegister( 0x12, 0x63 );
            this.WriteRegister( 0x03, 0x12 );
            this.WriteRegister( 0x32, 0xff );
            this.WriteRegister( 0x17, 0x16 );
            this.WriteRegister( 0x18, 0x02 );
            this.WriteRegister( 0x19, 0x01 );
            this.WriteRegister( 0x1a, 0x3d );
            this.WriteRegister( 0x36, 0xfa );
            this.WriteRegister( 0x69, 0x0a );
            this.WriteRegister( 0x8c, 0x8d );
            this.WriteRegister( 0xc0, 0xaa );
            this.WriteRegister( 0x40, 0xd0 );
            this.WriteRegister( 0x43, 0x14 );
            this.WriteRegister( 0x44, 0xf0 );
            this.WriteRegister( 0x45, 0x46 );
            this.WriteRegister( 0x46, 0x62 );
            this.WriteRegister( 0x47, 0x2a );
            this.WriteRegister( 0x48, 0x3c );
            this.WriteRegister( 0x59, 0x85 );
            this.WriteRegister( 0x5a, 0xa9 );
            this.WriteRegister( 0x5b, 0x64 );
            this.WriteRegister( 0x5c, 0x84 );
            this.WriteRegister( 0x5d, 0x53 );
            this.WriteRegister( 0x5e, 0x0e );
            this.WriteRegister( 0x6c, 0x0c );
            this.WriteRegister( 0xc6, 0x85 );
            this.WriteRegister( 0xcb, 0xf0 );
            this.WriteRegister( 0xcc, 0xd8 );
            this.WriteRegister( 0x71, 0x78 );
            this.WriteRegister( 0xa5, 0x68 );
            this.WriteRegister( 0x6f, 0x9e );
            this.WriteRegister( 0x42, 0xc0 );
            this.WriteRegister( 0x3f, 0x82 );
            this.WriteRegister( 0x8a, 0x23 );
            this.WriteRegister( 0x14, 0x3a );
            this.WriteRegister( 0x3b, 0xcc );
            this.WriteRegister( 0x34, 0x3d );
            this.WriteRegister( 0x41, 0x40 );
            this.WriteRegister( 0xc9, 0xe0 );
            this.WriteRegister( 0xca, 0xe8 );
            this.WriteRegister( 0xcd, 0x93 );
            this.WriteRegister( 0x7a, 0x20 );
            this.WriteRegister( 0x7b, 0x1c );
            this.WriteRegister( 0x7c, 0x28 );
            this.WriteRegister( 0x7d, 0x3c );
            this.WriteRegister( 0x7e, 0x5a );
            this.WriteRegister( 0x7f, 0x68 );
            this.WriteRegister( 0x80, 0x76 );
            this.WriteRegister( 0x81, 0x80 );
            this.WriteRegister( 0x82, 0x88 );
            this.WriteRegister( 0x83, 0x8f );
            this.WriteRegister( 0x84, 0x96 );
            this.WriteRegister( 0x85, 0xa3 );
            this.WriteRegister( 0x86, 0xaf );
            this.WriteRegister( 0x87, 0xc4 );
            this.WriteRegister( 0x88, 0xd7 );
            this.WriteRegister( 0x89, 0xe8 );
            this.WriteRegister( 0x4f, 0x98 );
            this.WriteRegister( 0x50, 0x98 );
            this.WriteRegister( 0x51, 0x00 );
            this.WriteRegister( 0x52, 0x28 );
            this.WriteRegister( 0x53, 0x70 );
            this.WriteRegister( 0x54, 0x98 );
            this.WriteRegister( 0x58, 0x1a );
            this.WriteRegister( 0x6b, 0x5a );
            this.WriteRegister( 0x90, 0x92 );
            this.WriteRegister( 0x91, 0x92 );
            this.WriteRegister( 0x9f, 0x90 );
            this.WriteRegister( 0xa0, 0x90 );
            this.WriteRegister( 0x16, 0x24 );
            this.WriteRegister( 0x2a, 0x00 );
            this.WriteRegister( 0x2b, 0x00 );
            this.WriteRegister( 0xac, 0x80 );
            this.WriteRegister( 0xad, 0x80 );
            this.WriteRegister( 0xae, 0x80 );
            this.WriteRegister( 0xaf, 0x80 );
            this.WriteRegister( 0xb2, 0xf2 );
            this.WriteRegister( 0xb3, 0x20 );
            this.WriteRegister( 0xb4, 0x20 );
            this.WriteRegister( 0xb6, 0xaf );
            this.WriteRegister( 0x29, 0x15 );
            this.WriteRegister( 0x9d, 0x02 );
            this.WriteRegister( 0x9e, 0x02 );
            this.WriteRegister( 0x9e, 0x02 );
            this.WriteRegister( 0x04, 0x03 );
            this.WriteRegister( 0x05, 0x2e );
            this.WriteRegister( 0x06, 0x2e );
            this.WriteRegister( 0x07, 0x2e );
            this.WriteRegister( 0x08, 0x2e );
            this.WriteRegister( 0x2f, 0x2e );
            this.WriteRegister( 0x4a, 0xe9 );
            this.WriteRegister( 0x4b, 0xdd );
            this.WriteRegister( 0x4c, 0xdd );
            this.WriteRegister( 0x4d, 0xdd );
            this.WriteRegister( 0x4e, 0xdd );
            this.WriteRegister( 0x70, 0x06 );
            this.WriteRegister( 0xa6, 0x40 );
            this.WriteRegister( 0xbc, 0x02 );
            this.WriteRegister( 0xbd, 0x01 );
            this.WriteRegister( 0xbe, 0x02 );
            this.WriteRegister( 0xbf, 0x01 );

        }

        private void Reset() {
            this.WriteRegister(0x12, 0x80);

            Thread.Sleep(100);
        }

        private void WriteRegister(byte register, byte value) => this.i2cDevice.Write(new byte[] { register, value });
        private byte ReadRegister(byte register) {
            var dataW = new byte[] { register };
            var dataR = new byte[1];

            this.i2cDevice.WriteRead(dataW, dataR);
            return dataR[0];

        }

        public enum Resolution {
            Vga = 0,
            Qvga = 1,
            Qqvga = 2
        }

    }
}
