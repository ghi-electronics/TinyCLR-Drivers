using System;
using System.Collections;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Gpio.Provider;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Interrupt;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Io;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Device
{
	internal abstract class Mcp23GpioProviderBase : IGpioControllerProvider
	{
		private readonly int _portCount;
		private readonly Mcp23Interrupt _interrupt;
		private readonly IDictionary _pinMap;

		protected Mcp23GpioProviderBase(DeviceSettings deviceSettings, GpioPin reset = null, GpioPin interruptPin = null)
		{
			this._portCount = deviceSettings.PortCount;
			Mcp23Core mcp23SCore = deviceSettings is SpiDeviceSettings ? new Mcp23SpiCore(deviceSettings) : new Mcp23I2CCore(deviceSettings);

			if (reset is not null)
			{
				reset.SetDriveMode(GpioPinDriveMode.Output);
				reset.Write(GpioPinValue.High);
				reset.Write(GpioPinValue.Low);
				reset.Write(GpioPinValue.High);

				Mcp23Core.Reset();
			}

			// implementation details
			mcp23SCore.SetRegisterMapping(default); // set register mapping to BANK = 0, redundant
			mcp23SCore.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.SeqOp, true); // Disable Sequential Address Mode

			this.Io = new Mcp23Io(mcp23SCore, this._portCount);
			this._pinMap = new Hashtable();

			if (interruptPin is not null)
			{
				this._interrupt = new Mcp23Interrupt(mcp23SCore, this._portCount, interruptPin);
				this._interrupt.PinChange += (_, args) =>
				{
					var handler = default(GpioPinValueChangedEventHandler);

					var pin = Mcp23Core.ResolvePin(args.Port, args.Bit);
					lock (this._pinMap)
						if (this._pinMap.Contains(pin) && this._pinMap[pin] is GpioPinValueChangedEventHandler eventHandler)
							handler = eventHandler;

					handler?.Invoke(null, new GpioPinValueChangedEventArgs(args.Value == GpioPinValue.High ? GpioPinEdge.RisingEdge : GpioPinEdge.FallingEdge, args.Timestamp));
				};
			}
		}

		internal Mcp23Io Io { get; }

		#region Implementation of IGpioExtender

		public int PinCount => this._portCount * 8;

		public void OpenPin(int pin)
		{
			if (pin < default(int) || pin >= this.PinCount)
				throw new ArgumentOutOfRangeException(nameof(pin), "Pin is out of range and cannot be opened.");

			lock (this._pinMap)
				this._pinMap.Add(pin, null); // also throw if pin already added, apparently what framework wants.
		}

		public void ClosePin(int pin)
		{
			lock (this._pinMap)
				if (this._pinMap.Contains(pin))
					this._pinMap.Remove(pin);
		}

		public void SetPinChangedHandler(int pin, GpioPinEdge edge, GpioPinValueChangedEventHandler value)
		{
			if (this._interrupt is not null)
			{
				lock (this._pinMap)
					this._pinMap[pin] = value;

				var port = Mcp23Core.ResolvePortAndBit(pin, out var bit);
				this._interrupt.SetPinInterruptMode(port, bit, edge);
				this._interrupt.EnablePinChangeInterrupt(port, bit, true);
			}
		}

		public void ClearPinChangedHandler(int pin)
		{
			if (this._interrupt is not null)
			{
				var port = Mcp23Core.ResolvePortAndBit(pin, out var bit);
				this._interrupt.EnablePinChangeInterrupt(port, bit, false);

				lock (this._pinMap)
					this._pinMap[pin] = default(GpioPinValueChangedEventHandler);
			}
		}

		public TimeSpan GetDebounceTimeout(int pin) => this._interrupt != null ? this._interrupt.GetDebounceTimeout(Mcp23Core.ResolvePortAndBit(pin, out var bit), bit) : TimeSpan.FromMilliseconds(-1);

		public void SetDebounceTimeout(int pin, TimeSpan timeSpan) => this._interrupt?.SetDebounceTimeout(Mcp23Core.ResolvePortAndBit(pin, out var bit), bit, timeSpan);

		public virtual bool IsDriveModeSupported(int pin, GpioPinDriveMode mode) => mode is GpioPinDriveMode.InputPullUp or GpioPinDriveMode.Input or GpioPinDriveMode.Output;

		public virtual GpioPinDriveMode GetDriveMode(int pin)
		{
			var port = Mcp23Core.ResolvePortAndBit(pin, out var bit);
			var mode = this.Io.GetPinMode(port, bit);
			return mode == Mcp23Core.PinMode.Output ? GpioPinDriveMode.Output : this.Io.IsPinPullUp(port, bit) ? GpioPinDriveMode.InputPullUp : GpioPinDriveMode.Input;
		}

		public virtual void SetDriveMode(int pin, GpioPinDriveMode mode)
		{
			var port = Mcp23Core.ResolvePortAndBit(pin, out var bit);
			switch (mode)
			{
				case GpioPinDriveMode.Input:
					this.Io.SetPinMode(port, bit, Mcp23Core.PinMode.Input);
					this.Io.EnablePinPullUp(port, bit, false);
					break;
				case GpioPinDriveMode.Output:
					this.Io.SetPinMode(port, bit, Mcp23Core.PinMode.Output);
					this.Io.EnablePinPullUp(port, bit, true);
					break;
				case GpioPinDriveMode.InputPullUp:
					this.Io.SetPinMode(port, bit, Mcp23Core.PinMode.Input);
					this.Io.EnablePinPullUp(port, bit, true);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), "gpio extender does not support this drive mode");
			}
		}

		public GpioPinValue Read(int pin) => this.Io.ReadPin(Mcp23Core.ResolvePortAndBit(pin, out var bit), bit);

		public void Write(int pin, GpioPinValue value) => this.Io.WritePin(Mcp23Core.ResolvePortAndBit(pin, out var bit), bit, value);

		public void TransferFeature(int pinSource, int pinDestination, uint mode, uint type, uint direction, uint speed, uint alternate) => throw new NotImplementedException();

		#endregion

		#region Implementation of IDisposable

		public void Dispose()
		{
			// ToDo implement IDisposable in each object, and call from here
			// dispose of everything here
		}

		#endregion
	}
}
