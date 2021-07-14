using System;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles


namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core
{
	internal abstract class Mcp23Core
	{
		private static RegisterMapping _registerMapping;

		internal enum RegisterMapping
		{
			Paired,
			Segregated
		}

		internal enum Port
		{
			A,
			B
		}

		internal enum PinMode
		{
			Output,
			Input
		}

		internal static void Reset() => _registerMapping = default;

		internal bool ReadBit(ControlRegister register, Port port, byte bit) => this.ReadBitValue(register, port, bit) > 0;

		internal byte ReadBitValue(ControlRegister register, Port port, byte bit) => (byte)(this.ReadRegister(register, port) & ResolveBitMask(bit));

		internal byte ReadRegister(ControlRegister register, Port port) => this.ReadRegister(ResolveRegister(register, port));

		// overload to avoid upstream parameter comparisons. slow?
		internal void WriteBit(ControlRegister register, Port port, byte bit, Enum value) => this.WriteBit(register, port, bit, Convert.ToByte($"{value}") != default);

		internal void WriteBit(ControlRegister register, Port port, byte bit, bool value)
		{
			var reg = ResolveRegister(register, port);
			if (value)
				this.WriteRegister(reg, (byte)(this.ReadRegister(reg) | ResolveBitMask(bit)));
			else
				this.WriteRegister(reg, (byte)(this.ReadRegister(reg) & ~ResolveBitMask(bit)));
		}

		internal void WriteRegister(ControlRegister register, Port port, byte value)
		{
			switch (register)
			{
				case ControlRegister.IntCap:
				case ControlRegister.IntF:
					throw new ArgumentException($"{register}", "is read-only");
			}

			this.WriteRegister(ResolveRegister(register, port), value);
		}

		internal void SetRegisterMapping(RegisterMapping mapping)
		{
			if (mapping != _registerMapping)
			{
				this.WriteBit(ControlRegister.IoCon, default, (byte)ConfigurationBit.Bank, mapping);
				_registerMapping = mapping;
			}
		}

		internal static Port ResolvePortAndBit(int pinNumber, out byte bit)
		{
			bit = (byte)(pinNumber % 8);
			return (Port)(pinNumber / 8);
		}

		internal static int ResolvePin(Port port, byte bit) => (8 * (int)port) + bit;

		private protected abstract byte ReadRegister(byte register);

		private protected abstract void WriteRegister(byte register, byte value);

		private static byte ResolveRegister(ControlRegister register, Port portName)
		{
			var reg = (byte)register;
			var port = (byte)portName;
			return (byte)(_registerMapping == RegisterMapping.Paired ? (reg * 2) + port : reg | (byte)(port << 4));
		}

		private static byte ResolveBitMask(byte bit) => (byte)(1 << (bit % 8));
	}
}
