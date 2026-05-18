using System;
using System.Collections;
using System.Text;
using System.Threading;

// Public surface mirrors GHIElectronics.TinyCLR.Drivers.Microchip.Enc28J60\Enc28J60Interface.cs.
// The Desktop shim cannot reach the physical ENC28J60 chip, so the helpers
// return inert values rather than throwing.
namespace GHIElectronics.TinyCLR.Drivers.Microchip.Enc28J60 {
    public static class Enc28J60Interface {
        public static void SoftReset() { }
        public static int TransmitErrorCounter() => 0;
        public static int ReceiveErrorCounter() => 0;
    }
}
