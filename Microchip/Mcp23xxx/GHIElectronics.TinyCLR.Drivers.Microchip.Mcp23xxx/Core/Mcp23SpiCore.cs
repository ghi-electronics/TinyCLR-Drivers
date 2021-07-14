using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core
{
    internal class Mcp23SpiCore : Mcp23Core
    {
        private const byte WriteOpcode = 0x40;
        private const byte ReadOpcode = 0x41;
        private readonly SpiDevice _spiDevice;
        private readonly GpioPin _chipSelect;

        /// <summary> Opcode, Start Register, value </summary>
        private static readonly byte[] Tx32 = new byte[4];
        private static readonly byte[] Tx24 = new byte[3];
        private static readonly byte[] Rx32 = new byte[4];
        private static readonly byte[] Rx24 = new byte[3];

        internal Mcp23SpiCore(DeviceSettings deviceSettings)
        {
            var spiExpanderOptions = (SpiDeviceSettings)deviceSettings;

            this._chipSelect = spiExpanderOptions.ChipSelect ?? throw new ArgumentNullException(nameof(spiExpanderOptions.ChipSelect), "This implementation manages its own chip Select line and cannot be null");
            this._chipSelect.SetDriveMode(GpioPinDriveMode.Output);
            this._chipSelect.Write(GpioPinValue.High);

            var spiConnectionSettings = new SpiConnectionSettings { ClockFrequency = spiExpanderOptions.Frequency };
            this._spiDevice = spiExpanderOptions.Controller.GetDevice(spiConnectionSettings);
        }

        #region Overrides of Mcp23Core

        private protected override byte ReadRegister(byte register)
        {
            this._chipSelect?.Write(GpioPinValue.Low);
            Tx24[0] = ReadOpcode;
            Tx24[1] = register;
            Tx24[2] = 0;
            this._spiDevice.TransferFullDuplex(Tx24, Rx24);
            this._chipSelect?.Write(GpioPinValue.High);
            return Rx24[2];
        }

        private protected override void WriteRegister(byte register, byte value)
        {
            this._chipSelect?.Write(GpioPinValue.Low);
            Tx24[0] = WriteOpcode;
            Tx24[1] = register;
            Tx24[2] = value;
            this._spiDevice.Write(Tx24);
            this._chipSelect?.Write(GpioPinValue.High);
        }

        #endregion
    }
}
