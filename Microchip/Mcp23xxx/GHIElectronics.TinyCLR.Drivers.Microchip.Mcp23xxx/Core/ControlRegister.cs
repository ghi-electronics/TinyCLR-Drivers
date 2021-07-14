namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core
{
    internal enum ControlRegister : byte
    {
        /// <summary> I/O Direction </summary>
        /// <remarks>
        ///     Controls the direction of the data I/O. When a bit is set, the corresponding pin becomes an input.
        ///		When a bit is clear, the corresponding pin becomes an output.
        /// </remarks>
        IoDir = 0x00,

        /// <summary> Input Polarity </summary>
        /// <remarks>
        ///		Allows the user to configure the polarity on the corresponding GPIO port bits.
        ///		If a bit is set, the corresponding GPIO register bit will reflect the inverted value on the pin.
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        IPol = 0x01,

        /// <summary> Interrupt on change control </summary>
        /// <remarks>
        ///     Controls the interrupt-on-change feature for each pin. If a bit is set, the corresponding pin is enabled for interrupt-on-change.
        ///		The DEFVAL and INTCON registers must also be configured if any pins are enabled for interrupt-on-change.
        /// </remarks>
        GpIntEn = 0x02,

        /// <summary> Default compare for interrupt on change </summary>
        /// <remarks>
        ///     The default comparison value is configured in the DEFVAL register. If enabled (via GPINTEN and INTCON) to
        ///     compare against the DEFVAL register, an opposite value on the associated pin will cause an interrupt to occur.
        /// </remarks>
        DefVal = 0x03,

        /// <summary> Interrupt Control </summary>
        /// <remarks>
        ///     Controls how the associated pin value is compared for the interrupt-on-change feature.
        ///		If a bit is set, the corresponding I/O pin is compared against the associated bit in the DEFVAL register.
        ///		If a bit value is clear, the corresponding I/O pin is compared against the previous value.
        /// </remarks>
        IntCon = 0x04,

        /// <summary> I/O Expander Configuration </summary>
        /// <remarks>
        /// bit 7 BANK: Controls how the registers are addressed (see Figure 1-4 and Figure 1-5)
        ///	    1 = The registers associated with each port are separated into different banks
        ///	    0 = The registers are in the same bank (addresses are sequential)
        /// bit 6 MIRROR: INT pins mirror bit
        ///		1 = The INT pins are internally connected in a wired OR configuration
        ///		0 = The INT pins are not connected. INTA is associated with Port A and INTB is associated with Port B
        /// bit 5 SEQOP: Sequential Operation mode bit.
        ///		1 = Sequential operation disabled, address pointer does not increment.
        ///		0 = Sequential operation enabled, address pointer increments.
        /// bit 4 Unimplemented: Reads as 0
        /// bit 3 Unimplemented: Reads as 0
        /// bit 2 ODR: Configures the INT pin as an open-drain output.
        ///		1 = Open-drain output (overrides the INTPOL bit).
        ///		0 = Active driver output (INTPOL bit sets the polarity).
        /// bit 1 INTPOL: Sets the polarity of the INT output pin.
        ///		1 =  Active-high.
        ///		0 =  Active-low.bit 0
        /// INTCC: Interrupt Clearing Control
        ///		1 = Reading INTCAP register clears the interrupt
        ///		0 = Reading GPIO register clears the interrupt
        /// </remarks>
        IoCon = 0x05,

        /// <summary> Pull Up resistor configuration register </summary>
        /// <remarks>
        ///     Controls the pull-up resistors for the port pins. If a bit is set and the corresponding pin is configured as an input,
        ///		the corresponding port pin is internally pulled up with a 100 kΩ resistor.
        /// </remarks>
        GpPu = 0x06,

        /// <summary> Interrupt Flag </summary>
        /// <remarks>
        ///     Reflects the interrupt condition on the port pins of any pin that is enabled for interrupts via the GPINTEN register
        ///		A ‘set’ bit indicates that the associated pin caused the interrupt. This register is ‘read-only’. Writes to this register will be ignored.
        /// </remarks>
        IntF = 0x07,

        /// <summary> Interrupt Capture </summary>
        /// <remarks>
        ///     Captures the GPIO port value at the time the interrupt occurred. The register is ‘read only’ and is updated only when an interrupt occurs.
        ///		The register will remain unchanged until the interrupt is cleared via a read of INTCAP or GPIO.
        /// </remarks>
        IntCap = 0x08,

        /// <summary> GPIO </summary>
        /// <remarks> Reflects the value on the port. Reading from this register reads the port. Writing to this register modifies the Output Latch (OLAT) register. </remarks>
        GpIo = 0x09,

        /// <summary> Output Latch </summary>
        /// <remarks>
        ///		Provides access to the output latches A read from this register results in a read of the OLAT and not the port itself.
        ///		A write to this register modifies the output latches that modifies the pins configured as outputs.
        /// </remarks>
        OLat = 0x0A
    }
}
