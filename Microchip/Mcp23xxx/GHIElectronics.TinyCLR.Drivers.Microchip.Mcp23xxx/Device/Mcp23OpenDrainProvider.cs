using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device
{
	internal class Mcp23OpenDrainProvider : Mcp23GpioProviderBase
	{
		internal Mcp23OpenDrainProvider(DeviceSettings deviceSettings, GpioPin reset = null, GpioPin interruptPin = null) : base(deviceSettings, reset, interruptPin) { }

		#region Overrides of Mcp23GpioProviderBase

		public override bool IsDriveModeSupported(int pin, GpioPinDriveMode mode) => mode is GpioPinDriveMode.OutputOpenDrain || base.IsDriveModeSupported(pin, mode);

		public override GpioPinDriveMode GetDriveMode(int pin)
		{
			var port = Mcp23Core.ResolvePortAndBit(pin, out var bit); // duplicate call in base branch, least expensive option

			if (!this.Io.IsPinPullUp(port, bit))
			{
				if (this.Io.GetPinMode(port, bit) == Mcp23Core.PinMode.Output)
					return GpioPinDriveMode.OutputOpenDrain;
			}

			return base.GetDriveMode(pin);
		}

		public override void SetDriveMode(int pin, GpioPinDriveMode mode)
		{
			if (mode == GpioPinDriveMode.OutputOpenDrain)
			{
				var port = Mcp23Core.ResolvePortAndBit(pin, out var bit);
				this.Io.SetPinMode(port, bit, Mcp23Core.PinMode.Output);
				this.Io.EnablePinPullUp(port, bit, false);
			}
			else
				base.SetDriveMode(pin, mode);
		}

		#endregion
	}
}
