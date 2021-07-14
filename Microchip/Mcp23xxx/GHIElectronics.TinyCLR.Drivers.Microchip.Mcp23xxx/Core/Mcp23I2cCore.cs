using System;

using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device;


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core
{
    internal class Mcp23I2CCore : Mcp23Core
    {
        #region Overrides of Mcp23Core

        internal Mcp23I2CCore(DeviceSettings deviceSettings)
        {
        }

        private protected override byte ReadRegister(byte register) => throw new NotImplementedException();

        private protected override void WriteRegister(byte register, byte value) => throw new NotImplementedException();

        #endregion
    }
}
