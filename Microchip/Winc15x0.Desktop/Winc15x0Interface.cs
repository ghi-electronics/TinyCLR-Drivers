using System;
using GHIElectronics.TinyCLR.Devices.Network;

// Public surface mirrors GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0\Winc15x0Interface.cs.
// No physical WINC1500 reachable on Desktop, so helpers return inert/safe values
// instead of throwing. FirmwareUpdate(byte[]) still throws because the impl
// also throws — it's an unsupported overload on both targets.
namespace GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0 {
    public static class Winc15x0Interface {

        public static readonly string[] FirmwareSupports = new string[] { "19.5.4.15567" };

        public static string GetFirmwareVersion() => "0.0.0.0";

        public static string[] Scan() => new string[0];

        public static int GetRssi() => 0;

        public static bool FirmwareUpdate(string url, TimeSpan timeout) => false;

        public static bool FirmwareUpdate(byte[] buffer) => FirmwareUpdate(buffer, 0, buffer.Length);

        public static bool FirmwareUpdate(byte[] buffer, int offset, int count) => throw new Exception("Not supported.");

        public static byte[] GetMacAddress() => new byte[6];

        public static void AddMulticastMacAddress(byte[] multicastMacAddress) {
            if (multicastMacAddress == null)
                throw new ArgumentNullException();

            if (multicastMacAddress.Length != 6)
                throw new ArgumentException("Invalid argument.");
        }

        public static void RemoveMulticastMacAddress(byte[] multicastMacAddress) {
            if (multicastMacAddress == null)
                throw new ArgumentNullException();

            if (multicastMacAddress.Length != 6)
                throw new ArgumentException("Invalid argument.");
        }
    }
}
