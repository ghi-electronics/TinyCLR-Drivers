using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Gpio.Provider;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Spi;

using static GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Mcp23Xxx;
using static GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Mcp23Xxx.Product;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device
{
	public class Mcp23GpioProvider : IGpioControllerProvider
	{
		private readonly IGpioControllerProvider _ioExtender;

		public Mcp23GpioProvider(Product product, SpiController controller, GpioPin chipSelect, int frequency = 1_000_000, GpioPin reset = null, GpioPin interruptPin = null)
		{
			var connectionSettings = new SpiDeviceSettings(product, chipSelect, controller, frequency);
			this._ioExtender = connectionSettings.ThisProduct switch
			{
				Mcp23X09 or Mcp23X18 => new Mcp23OpenDrainProvider(connectionSettings, reset, interruptPin),
				Mcp23X08 or Mcp23X17 => new Mcp23ActiveDriveProvider(connectionSettings, reset, interruptPin),
				_ => throw new ArgumentOutOfRangeException(nameof(connectionSettings.ThisProduct), "Device unsupported")
			};
		}

		public Mcp23GpioProvider(Product product, I2cController controller, GpioPin address = null, int frequency = 400_000, GpioPin reset = null, GpioPin interruptPin = null)
		{
			var connectionSettings = new I2CDeviceSettings(product, address, controller, frequency);
			this._ioExtender = connectionSettings.ThisProduct switch
			{
				Mcp23X09 or Mcp23X18 => new Mcp23OpenDrainProvider(connectionSettings, reset, interruptPin),
				Mcp23X08 or Mcp23X17 => new Mcp23ActiveDriveProvider(connectionSettings, reset, interruptPin),
				_ => throw new ArgumentOutOfRangeException(nameof(connectionSettings.ThisProduct), "Device unsupported")
			};
		}

		#region Implementation of IGpioControllerProvider

		public int PinCount => this._ioExtender.PinCount;

		public void OpenPin(int pin) => this._ioExtender.OpenPin(pin);

		public void ClosePin(int pin) => this._ioExtender.ClosePin(pin);

		public void SetPinChangedHandler(int pin, GpioPinEdge edge, GpioPinValueChangedEventHandler value) => this._ioExtender.SetPinChangedHandler(pin, edge, value);

		public void ClearPinChangedHandler(int pin) => this._ioExtender.ClearPinChangedHandler(pin);

		public TimeSpan GetDebounceTimeout(int pin) => this._ioExtender.GetDebounceTimeout(pin);

		public void SetDebounceTimeout(int pin, TimeSpan value) => this._ioExtender.SetDebounceTimeout(pin, value);

		public bool IsDriveModeSupported(int pin, GpioPinDriveMode mode) => this._ioExtender.IsDriveModeSupported(pin, mode);

		public GpioPinDriveMode GetDriveMode(int pin) => this._ioExtender.GetDriveMode(pin);

		public void SetDriveMode(int pin, GpioPinDriveMode mode) => this._ioExtender.SetDriveMode(pin, mode);

		public GpioPinValue Read(int pin) => this._ioExtender.Read(pin);

		public void Write(int pin, GpioPinValue value) => this._ioExtender.Write(pin, value);

		public void TransferFeature(int pinSource, int pinDestination, uint mode, uint type, uint direction, uint speed, uint alternate) => throw new NotImplementedException();

		#endregion

		#region Implementation of IDisposable

		// ToDo implement Dispose
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
