using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Io
{
	internal class Mcp23Io
	{
		private readonly IoCore[] _ios;

		internal Mcp23Io(Mcp23Core mcp23Core, int portCount)
		{
			if (mcp23Core is null)
				throw new ArgumentNullException(nameof(mcp23Core), $"{nameof(mcp23Core)} cannot be null");

			this._ios = new IoCore[portCount];
			for (var port = 0; port < portCount; port++)
			{
				this._ios[port] = new IoCore(mcp23Core, (Mcp23Core.Port)port);
			}
		}

		internal GpioPinValue ReadPin(Mcp23Core.Port port, byte pin) => this._ios[(byte)port].ReadPin(pin) ;
		internal void WritePin(Mcp23Core.Port port, byte pin, GpioPinValue value) => this._ios[(byte)port].WritePin(pin, value);

		internal void SetPinMode(Mcp23Core.Port port, byte pin, Mcp23Core.PinMode mode) => this._ios[(byte)port].SetPinMode(pin, mode);
		internal Mcp23Core.PinMode GetPinMode(Mcp23Core.Port port, byte pin) => this._ios[(byte)port].GetPinMode(pin);

		internal void InvertPinPolarity(Mcp23Core.Port port, byte pin, bool invert) => this._ios[(byte)port].InvertPinPolarity(pin, invert);

		internal void EnablePinPullUp(Mcp23Core.Port port, byte pin, bool enabled) => this._ios[(byte)port].EnablePinPullUp(pin, enabled);
		internal bool IsPinPullUp(Mcp23Core.Port port, byte pin) => this._ios[(byte)port].IsPinPullUp(pin);
	}
}
