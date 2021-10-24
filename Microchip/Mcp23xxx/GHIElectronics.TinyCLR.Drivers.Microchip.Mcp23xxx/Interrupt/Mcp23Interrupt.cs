using System;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Interrupt
{
	internal class Mcp23Interrupt
	{
		private readonly InterruptCore[] _interrupts;

		internal Mcp23Interrupt(Mcp23Core mcp23Core, int portCount, GpioPin interruptInputPin, InterruptDriveMode interruptDriveMode = InterruptDriveMode.OpenDrain, GpioPinValue interruptPolarity = default)
		{
			if (mcp23Core is null)
				throw new ArgumentNullException(nameof(mcp23Core), $"{nameof(mcp23Core)} cannot be null");

			if (interruptInputPin is null)
				throw new ArgumentNullException(nameof(interruptInputPin), "Assigned interrupt pin must be initialize to utilize the IO expander interrupt function");

			// ToDo remove options to control interrupt drive properties?
			mcp23Core.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.ODr, interruptDriveMode); // Set Interrupt Drive Mode
			if (interruptDriveMode == InterruptDriveMode.OpenDrain)
			{
				interruptInputPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
				interruptInputPin.ValueChangedEdge = GpioPinEdge.FallingEdge;
			}
			else
			{
				interruptInputPin.SetDriveMode(GpioPinDriveMode.Input);
				mcp23Core.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.IntPol, interruptPolarity); // Set Active Interrupt Polarity, no need if Drive Mode Open Drain
				interruptInputPin.ValueChangedEdge = interruptPolarity == GpioPinValue.High ? GpioPinEdge.RisingEdge : GpioPinEdge.FallingEdge;
			}

			interruptInputPin.DebounceTimeout = TimeSpan.FromMilliseconds(0); // needed as default is set to 20ms

			mcp23Core.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.IntCC, InterruptClearRegister.IntCap); // Set Interrupt Flag Clear Control
			mcp23Core.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.Mirror, true); // mirror interrupts allows use of either or interrupt pin

			this._interrupts = new InterruptCore[portCount];
			for (var p = 0; p < portCount; p++)
				this._interrupts[p] = new InterruptCore(mcp23Core, (Mcp23Core.Port)p);

			interruptInputPin.ValueChanged += (_, _) =>
			{
				foreach (var interrupt in this._interrupts)
					interrupt.OnInterruptInputPinValueChanged();
			};
		}

		internal event InterruptEventHandler PinChange
		{
			add
			{
				foreach (var interrupt in this._interrupts)
				{
					// clean but a hack to ensure only 1 subscribed event handler.
					// Not too concerned missing an interrupt as this should be done once on initialization and only removed if all pins closed? ToDo add capability to remove handler after all pins released?
					interrupt.PinChange -= value;
					interrupt.PinChange += value;
				}
			}
			remove
			{
				foreach (var interrupt in this._interrupts)
					interrupt.PinChange -= value;
			}
		}

		internal enum InterruptDriveMode
		{
			ActiveDriver,
			OpenDrain,
		}

		private enum InterruptClearRegister
		{
			Gpio,
			IntCap
		}

		internal TimeSpan GetDebounceTimeout(Mcp23Core.Port port, byte pin) => this._interrupts[(byte)port].GetDebounceTimeout(pin);

		internal void SetDebounceTimeout(Mcp23Core.Port port, byte pin, TimeSpan value) => this._interrupts[(byte)port].SetDebounceTimeout(pin, value);

		internal void SetPinInterruptMode(Mcp23Core.Port port, byte pin, GpioPinEdge edge) => this._interrupts[(byte)port].SetPinInterruptMode(pin, edge);

		internal void EnablePinChangeInterrupt(Mcp23Core.Port port, byte pin, bool enable) => this._interrupts[(byte)port].EnableInterruptOnPinChange(pin, enable);
	}
}
