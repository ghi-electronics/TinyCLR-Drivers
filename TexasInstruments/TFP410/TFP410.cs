using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Display;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.TFP410 {
    public enum Resolution {
        SVGA,
        QHD,
        XGA,
        HD720p
    }
    public class TFP410Controller {

        const byte DeviceAddress = 0x38;
        private GpioPin resetPin;
        private I2cDevice i2cDevice;
        public ParallelDisplayControllerSettings Configuration { get; }
        public TFP410Controller(I2cController i2cController, Resolution resolutuion) : this (i2cController, resolutuion, DisplayOrientation.Degrees0) { }
        public TFP410Controller(I2cController i2cController, Resolution resolutuion, DisplayOrientation orientation) : this (i2cController, resolutuion, orientation, null) { }        
        public TFP410Controller(I2cController i2cController, Resolution resolutuion, DisplayOrientation orientation = DisplayOrientation.Degrees0, GpioPin resetPin = null) {
            var setting = new I2cConnectionSettings(DeviceAddress) {
                BusSpeed = 100000,
                AddressFormat = I2cAddressFormat.SevenBit
            };

            this.i2cDevice = i2cController.GetDevice(setting);
            this.resetPin = resetPin;

            this.Reset();

            this.Configuration = this.GetSetting(resolutuion, orientation);
        }

        private void Reset() {
            if (this.resetPin != null) {
                this.resetPin.SetDriveMode(GpioPinDriveMode.Output);
                this.resetPin.Write(GpioPinValue.Low);
                Thread.Sleep(50);
                this.resetPin.Write(GpioPinValue.High);
                Thread.Sleep(50);
            }

            var buff1 = new byte[5];
            var buff2 = new byte[5];

            buff1[0] = 0;

            if (this.WriteToReg(buff1, 1, buff2, 5) == false)
                throw new Exception("Couldn't communicate to the controller.");
            if (this.WriteToReg(buff1, 1, buff2, 5) == false)
                throw new Exception("Couldn't communicate to the controller.");
            if (this.WriteToReg(buff1, 1, buff2, 5) == false)
                throw new Exception("Couldn't communicate to the controller.");

            buff1[0] = 0x08;
            buff1[1] = 0x35;
            buff1[2] = 0x01 | 0x04 | 0x08 | 0x30;
            buff1[3] = 0x80;

            if (this.WriteToReg(buff1, 4, null, 0) == false)
                throw new Exception("Couldn't communicate to the controller.");

        }

        private bool WriteToReg(byte[] pBuf, ushort len, byte[] pBuf2, ushort len2) {
            try {
                if (pBuf2 != null)
                    this.i2cDevice.WriteRead(pBuf, 0, len, pBuf2, 0, len2);
                else
                    this.i2cDevice.Write(pBuf);

                return true;
            }
            catch { }

            return false;
        }

        private ParallelDisplayControllerSettings GetSetting(Resolution resolution, DisplayOrientation orientation) {
            var controllerSetting = new ParallelDisplayControllerSettings {
                DataFormat = DisplayDataFormat.Rgb565,
                Orientation = orientation,

                PixelPolarity = false,
                DataEnablePolarity = false,
                DataEnableIsFixed = false,
                HorizontalSyncPolarity = false,
                VerticalSyncPolarity = false
            };

            switch (resolution) {
                case Resolution.SVGA:
                    controllerSetting.Width = 800;
                    controllerSetting.Height = 600;
                    controllerSetting.PixelClockRate = 36000000;
                    controllerSetting.HorizontalBackPorch = 64;
                    controllerSetting.HorizontalFrontPorch = 40;
                    controllerSetting.HorizontalSyncPulseWidth = 168;
                    controllerSetting.VerticalBackPorch = 23;
                    controllerSetting.VerticalFrontPorch = 1;
                    controllerSetting.VerticalSyncPulseWidth = 4;
                    break;

                case Resolution.QHD:
                    controllerSetting.Width = 960;
                    controllerSetting.Height = 540;
                    controllerSetting.PixelClockRate = 36000000;
                    controllerSetting.HorizontalBackPorch = 64;
                    controllerSetting.HorizontalFrontPorch = 40;
                    controllerSetting.HorizontalSyncPulseWidth = 168;
                    controllerSetting.VerticalBackPorch = 23;
                    controllerSetting.VerticalFrontPorch = 1;
                    controllerSetting.VerticalSyncPulseWidth = 4;
                    break;

                case Resolution.XGA:
                    controllerSetting.Width = 1024;
                    controllerSetting.Height = 768;
                    controllerSetting.PixelClockRate = 52000000;
                    controllerSetting.HorizontalBackPorch = 144;
                    controllerSetting.HorizontalFrontPorch = 40;
                    controllerSetting.HorizontalSyncPulseWidth = 104;
                    controllerSetting.VerticalBackPorch = 18;
                    controllerSetting.VerticalFrontPorch = 3;
                    controllerSetting.VerticalSyncPulseWidth = 4;
                    break;

                case Resolution.HD720p:
                    controllerSetting.Width = 1280;
                    controllerSetting.Height = 720;
                    controllerSetting.PixelClockRate = 60500000;
                    controllerSetting.HorizontalBackPorch = 176;
                    controllerSetting.HorizontalFrontPorch = 48;
                    controllerSetting.HorizontalSyncPulseWidth = 128;
                    controllerSetting.VerticalBackPorch = 16;
                    controllerSetting.VerticalFrontPorch = 3;
                    controllerSetting.VerticalSyncPulseWidth = 5;
                    break;
            }

            return controllerSetting;
        }
    }
}
