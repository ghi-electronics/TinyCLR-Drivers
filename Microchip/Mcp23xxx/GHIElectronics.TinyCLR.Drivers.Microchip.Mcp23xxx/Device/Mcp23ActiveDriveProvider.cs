using GHIElectronics.TinyCLR.Devices.Gpio;


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device
{
    internal class Mcp23ActiveDriveProvider : Mcp23GpioProviderBase
    {
        internal Mcp23ActiveDriveProvider(DeviceSettings deviceSettings, GpioPin reset = null, GpioPin interruptPin = null) : base(deviceSettings, reset, interruptPin) { }
    }
}
