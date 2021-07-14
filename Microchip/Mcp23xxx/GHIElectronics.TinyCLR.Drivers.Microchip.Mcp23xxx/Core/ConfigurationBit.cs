namespace GHIElectronics.TinyCLR.Drivers.Microchip.Mcp23xxx.Core
{
    internal enum ConfigurationBit
    {
        /// bit 0 INTCC: Interrupt Clearing Control
        ///		1 = Reading INTCAP register clears the interrupt
        ///		0 = Reading GPIO register clears the interrupt
        IntCC,

        /// bit 1 INTPOL: Sets the polarity of the INT output pin.
        ///		1 =  Active-high.
        ///		0 =  Active-low.
        IntPol,

        /// bit 2 ODR: Configures the INT pin as an open-drain output.
        ///		1 = Open-drain output (overrides the INTPOL bit).
        ///		0 = Active driver output (INTPOL bit sets the polarity).
        ODr,

        /// bit 3 Unimplemented: Reads as 0

        /// bit 4 Unimplemented: Reads as 0

        /// bit 5 SEQOP: Sequential Operation mode bit.
        ///		1 = Sequential operation disabled, address pointer does not increment.
        ///		0 = Sequential operation enabled, address pointer increments.
        SeqOp = 5,

        /// bit 6 MIRROR: INT pins mirror bit
        ///		1 = The INT pins are internally connected in a wired OR configuration
        ///		0 = The INT pins are not connected. INTA is associated with Port A and INTB is associated with Port B
        Mirror,

        /// bit 7 BANK: Controls how the registers are addressed (see Figure 1-4 and Figure 1-5)
        ///	    1 = The registers associated with each port are separated into different banks
        ///	    0 = The registers are in the same bank (addresses are sequential)
        Bank
    }
}
