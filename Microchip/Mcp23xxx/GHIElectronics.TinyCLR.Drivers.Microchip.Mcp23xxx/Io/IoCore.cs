using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Io
{
	internal class IoCore
	{
		private readonly Mcp23Core _mcp23Core;
		private readonly Mcp23Core.Port _port;

		internal IoCore(Mcp23Core mcp23Core, Mcp23Core.Port port)
		{
			this._mcp23Core = mcp23Core ?? throw new ArgumentNullException(nameof(mcp23Core), "cannot be null");
			this._port = port;
		}

		internal GpioPinValue ReadPin(byte pin) => this._mcp23Core.ReadBit(ControlRegister.GpIo, this._port, pin) ? GpioPinValue.High : GpioPinValue.Low;
		internal void WritePin(byte pin, GpioPinValue value) => this._mcp23Core.WriteBit(ControlRegister.GpIo, this._port, pin, value);

		internal void SetPinMode(byte pin, Mcp23Core.PinMode mode) => this._mcp23Core.WriteBit(ControlRegister.IoDir, this._port, pin, mode);
		internal Mcp23Core.PinMode GetPinMode(byte pin) => (Mcp23Core.PinMode)this._mcp23Core.ReadBitValue(ControlRegister.IoDir, this._port, pin);

		internal void InvertPinPolarity(byte pin, bool invert) => this._mcp23Core.WriteBit(ControlRegister.IPol, this._port, pin, invert);

		internal void EnablePinPullUp(byte pin, bool enabled) => this._mcp23Core.WriteBit(ControlRegister.GpPu, this._port, pin, enabled);
		internal bool IsPinPullUp(byte pin) => this._mcp23Core.ReadBit(ControlRegister.GpPu, this._port, pin);
	}
}
