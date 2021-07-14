using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Interrupt
{
	internal delegate void InterruptEventHandler(object source, InterruptEventArgs e);

	internal class InterruptEventArgs : EventArgs
	{
		internal InterruptEventArgs(Mcp23Core.Port port, byte bit, GpioPinValue value, DateTime timestamp)
		{
			this.Port = port;
			this.Bit = bit;
			this.Value = value;
			this.Timestamp = timestamp;
		}

		internal Mcp23Core.Port Port { get; }
		internal byte Bit { get; }
		internal GpioPinValue Value { get; }
		internal DateTime Timestamp { get; }
	}
}
