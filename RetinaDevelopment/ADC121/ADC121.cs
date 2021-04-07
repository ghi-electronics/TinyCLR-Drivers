using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace RetinaDevelopment.Drivers {
    public class Adc121
    {

        private I2cConnectionSettings settings;
        private I2cController controller;
        private I2cDevice device;

        //The ADC result is located inside of the register with address 0x00
        private readonly byte[] sendBuffer = { 0x00 };
        private byte[] resultBuffer = new byte[2];

        public Adc121(string i2C, int address, uint frequency = 400_000)
        {
            this.settings = new I2cConnectionSettings(address, frequency);
            this.controller = I2cController.FromName(i2C);
            this.device = this.controller.GetDevice(this.settings);
        }

        public int GetValue()
        {
            try
            {
                this.device.WriteRead(this.sendBuffer, this.resultBuffer);
                return ((this.resultBuffer[0] << 8) + this.resultBuffer[1]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Something went wrong while communicating with the ADC121!");
            }
            return -1;
        }
    }
}
