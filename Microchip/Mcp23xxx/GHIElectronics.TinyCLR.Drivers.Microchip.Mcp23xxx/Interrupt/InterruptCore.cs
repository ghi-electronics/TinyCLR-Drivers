using System;

using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Interrupt
{
	internal class InterruptCore
	{
		private static readonly TimeSpan InfiniteTimeSpan = TimeSpan.FromMilliseconds(-1);
		private readonly Mcp23Core _mcp23Core;
		private readonly Mcp23Core.Port _port;
		private readonly Timer[] _dBounceTimer = new Timer[8];
		private readonly TimeSpan[] _debounceTimeout = new TimeSpan[8];
		private byte _intCap, _lastCap;
		private InterruptEventHandler _pinChange;
		/// <summary>
		/// work around for devices' funky single edge interrupt mode.
		/// from spec: "Read GPIO or INTCAP (INT clears only if interrupt condition does not exist.)"
		/// wtf. need to continuously poll until bit changes back to DEF then clear the flag?
		/// no reason to use interrupt if need to poll.
		/// see spec FIGURE 1-12:
		/// </summary>
		private GpioPinEdge _edge;// = GpioPinEdge.FallingEdge | GpioPinEdge.RisingEdge;

		/// <summary>
		/// Implements an 8 bit interrupt port
		/// </summary>
		/// <param name="mcp23Core"></param>
		/// <param name="port"> extenders GPIO port </param>
		internal InterruptCore(Mcp23Core mcp23Core, Mcp23Core.Port port)
		{
			this._mcp23Core = mcp23Core ?? throw new ArgumentNullException(nameof(mcp23Core), "cannot be null");
			this._port = port;
			this._intCap = this._lastCap = this._mcp23Core.ReadRegister(ControlRegister.IntCap, this._port); // clear int flags
		}

		internal event InterruptEventHandler PinChange
		{
			add
			{
				// clean but a hack to ensure only 1 subscribed event handler.
				// Not too concerned missing an interrupt as this should be done once on initialization and only removed if all 8 pins closed?
				this._pinChange -= value;
				this._pinChange += value;
			}
			remove => this._pinChange -= value;
		}

		internal enum PinChangeCompareValue
		{
			Last,
			Default
		}

		internal TimeSpan GetDebounceTimeout(byte pin) => this._debounceTimeout[pin];

		internal void SetDebounceTimeout(byte pin, TimeSpan timeSpan) => this._debounceTimeout[pin] = timeSpan;

		/// <summary>
		/// Trigger port interrupt on any input changes or changes from a set default value
		/// </summary>
		/// <param name="pin">interrupt pin to set</param>
		/// <param name="edge">interrupt edge</param>
		internal void SetPinInterruptMode(byte pin, GpioPinEdge edge)
		{
			if (edge == (GpioPinEdge.FallingEdge | GpioPinEdge.RisingEdge)) // both edges
				this._mcp23Core.WriteBit(ControlRegister.IntCon, this._port, pin, PinChangeCompareValue.Last);
			else
			{
				this._mcp23Core.WriteBit(ControlRegister.IntCon, this._port, pin, PinChangeCompareValue.Default);
				this._mcp23Core.WriteBit(ControlRegister.DefVal, this._port, pin, edge == GpioPinEdge.FallingEdge ? GpioPinValue.High : GpioPinValue.Low);
			}

			/*this._edge = edge;
			this._mcp23Core.WriteBit(ControlRegister.IntCon, this._port, pin, PinChangeCompareValue.Last);*/
		}

		/// <summary>
		/// Enable a pin interrupt
		/// </summary>
		/// <param name="pin">interrupt pin to enable or disable</param>
		/// <param name="enable">enable</param>
		internal void EnableInterruptOnPinChange(byte pin, bool enable)
		{
			// avoid reinitializing the interrupt, specifically clearing the flag.
			// TinyCLR framework will force this call when setting the interrupt edge as well as subscribing to it
			switch (this._mcp23Core.ReadBit(ControlRegister.GpIntEn, this._port, pin))
			{
				case false when enable:
				{
					this._dBounceTimer[pin] ??= new Timer(b =>
					{
						var bit = (byte)b;
						var bitMask = (1 << bit);
						var intCap = this._intCap & bitMask;
						if ((intCap ^ (this._lastCap & bitMask)) > 0) // if bit changed
						{
							var bitValue = intCap > 0 ? GpioPinValue.High : GpioPinValue.Low;
							this._lastCap = bitValue == GpioPinValue.High ? (byte)(this._lastCap | bitMask) : (byte)(this._lastCap & ~bitMask);

							/*switch (this._edge)
							{
								case GpioPinEdge.RisingEdge when bitValue == GpioPinValue.High:
								case GpioPinEdge.FallingEdge when bitValue == GpioPinValue.Low:
								case GpioPinEdge.FallingEdge | GpioPinEdge.RisingEdge: // both rising & falling
									this._pinChange?.Invoke(this, new InterruptEventArgs(this._port, bit, bitValue, DateTime.Now));
									break;
							}*/

							this._pinChange?.Invoke(this, new InterruptEventArgs(this._port, bit, bitValue, DateTime.Now));
						}
					}, pin, InfiniteTimeSpan, InfiniteTimeSpan);

					// capture initial value of pin without clearing its or others interrupt flags
					var gpio = this._mcp23Core.ReadRegister(ControlRegister.GpIo, this._port) & (1 << pin);
					this._intCap = (byte)(this._intCap | gpio);
					this._lastCap = (byte)(this._lastCap | gpio);

					this._mcp23Core.WriteBit(ControlRegister.GpIntEn, this._port, pin, true);
					break;
				}
				case true when !enable:
				{
					if (this._dBounceTimer[pin] is not null)
						this._dBounceTimer[pin].Change(InfiniteTimeSpan, InfiniteTimeSpan);

					this._mcp23Core.WriteBit(ControlRegister.GpIntEn, this._port, pin, false);
					break;
				}
			}
		}

		// Bug: has threw twice due to reading IntF = 0xFF even though only one interrupt bit was enabled, which is not possible
		// Both times device was being handled, ESD error? probably not.
		/// <summary>
		/// Called from upon port change interrupt
		/// </summary>
		internal void OnInterruptInputPinValueChanged()
		{
			var intFlag = this._mcp23Core.ReadRegister(ControlRegister.IntF, this._port);
			this._intCap = this._mcp23Core.ReadRegister(ControlRegister.IntCap, this._port); // clear int flags

			for (var bit = 0; bit < 8; bit++)
			{
				if ((intFlag & (1 << bit)) > 0/* && this._dBounceTimer[bit] is not null*/) // if pin caused interrupt then reset its dBounce timer, and avoid above noted Bug ToDo see if this is still valid
					this._dBounceTimer[bit].Change(this._debounceTimeout[bit], InfiniteTimeSpan);
			}
		}
	}
}
