using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.Drivers.Microchip.Enc28J60 {
    public static class Enc28J60Interface {
        public static void SoftReset() => NativeSoftReset();
        public static int TransmitErrorCounter() => NativeTransmitErrorCounter();
        public static int ReceiveErrorCounter() => NativeReceiveErrorCounter();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeSoftReset();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int NativeTransmitErrorCounter();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int NativeReceiveErrorCounter();

    }

}
