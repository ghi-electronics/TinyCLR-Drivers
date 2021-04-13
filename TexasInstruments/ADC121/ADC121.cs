using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.TexasInstruments.ADC121 {
    public class Adc121
    {
        private I2cDevice device;

        //The ADC result is located inside of the register with address 0x00
        private readonly byte[] sendBuffer = { 0x00 };
        private byte[] resultBuffer = new byte[2];


        public Adc121(I2cDevice i2c) => this.device = i2c;

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
