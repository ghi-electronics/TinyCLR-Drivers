using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Spi;

using static GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Mcp23Xxx;
using static GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Mcp23Xxx.Product;


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device
{
    internal abstract class DeviceSettings
    {
        protected DeviceSettings(Product product)
        {
            this.ThisProduct = product;

            this.PortCount = product switch
            {
                Mcp23X08 or Mcp23X09 => 1,
                Mcp23X17 or Mcp23X18 => 2,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal int PortCount { get; }

        internal Product ThisProduct { get; }
    }

    internal class SpiDeviceSettings : DeviceSettings
    {
        internal SpiDeviceSettings(Product product, GpioPin chipSelect, SpiController controller, int frequency = 1_000_000) : base(product)
        {
            this.ChipSelect = chipSelect;
            this.Controller = controller;
            this.Frequency = frequency;
        }

        internal SpiController Controller { get; }
        internal GpioPin ChipSelect { get; }
        internal int Frequency { get; }
    }

    internal class I2CDeviceSettings : DeviceSettings
    {
        internal I2CDeviceSettings(Product product, GpioPin address, I2cController controller, int frequency = 400_000) : base(product)
        {

        }

        internal I2cController Controller { get; }
        internal GpioPin ChipSelect { get; }
        internal int Frequency { get; }
    }
}
