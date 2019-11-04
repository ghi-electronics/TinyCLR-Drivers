using System;
using System.Runtime.CompilerServices;
using GHIElectronics.TinyCLR.Devices.Network;

namespace GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0 {
    public static class Winc15x0Interface {

        private static bool initialized = false;

        public static readonly string[] FirmwareSupports = new string[] { "19.5.4.15567" };

        public static string GetFirmwareVersion() {
            TurnOn();

            NativeReadFirmwareVersion(out var ver1, out var ver2);

            var major = (ver1 >> 16) & 0xFF;
            var minor = (ver1 >> 8) & 0xFF;
            var patch = (ver1 >> 0) & 0xFF;

            return $"{major}.{minor}.{patch}.{ver2}";
        }

        public static bool FirmwareUpdate(string url, int timeout) {
            TurnOn();

            return NativeFirmwareUpdatebyOta(url, timeout);
        }

        public static bool FirmwareUpdate(byte[] buffer) => FirmwareUpdate(buffer, 0, buffer.Length);

        public static bool FirmwareUpdate(byte[] buffer, int offset, int count) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(buffer));

            TurnOn();

            return NativeFirmwareUpdate(buffer, offset, count);
        }

        private static void TurnOn() {
            if (!initialized) {
                if (NativeTurnOn()) {
                    initialized = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool NativeTurnOn();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool NativeFirmwareUpdatebyOta(string url, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool NativeFirmwareUpdate(byte[] data, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeReadFirmwareVersion(out uint ver1, out uint ver2);
    }
}
