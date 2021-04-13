using System;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace GHIElectronics.TinyCLR.Drivers.Microchip.MCP4725 {
    public class Mcp4725
    {

        private I2cDevice device;

        //The DAC value is located inside of the registers with address 0x40 and 0x41, so we start at 0x40
        private readonly byte[] sendBuffer = { 0x40, 0x00, 0x00 };

        public Mcp4725(I2cDevice i2c) => this.device = i2c;

        public void SetValue(int value)
        {
            this.sendBuffer[1] = (byte)(value / 16);
            this.sendBuffer[2] = (byte)((value % 16) << 4);

            try
            {
                this.device.Write(this.sendBuffer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Something went wrong while communicating with the MCP4725!");
            }
        }
    }
}
