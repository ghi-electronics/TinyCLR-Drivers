using System;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace RetinaDevelopment.Drivers
{
    public class Mcp4725
    {

        private I2cConnectionSettings settings;
        private I2cController controller;
        private I2cDevice device;

        //The DAC value is located inside of the registers with address 0x40 and 0x41, so we start at 0x40
        private readonly byte[] sendBuffer = { 0x40, 0x00, 0x00 };

        public Mcp4725(string i2C, int address, uint frequency = 400_000)
        {
            this.settings = new I2cConnectionSettings(address, frequency);
            this.controller = I2cController.FromName(i2C);
            this.device = this.controller.GetDevice(this.settings);
        }

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
