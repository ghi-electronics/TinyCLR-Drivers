using System;
using System.Runtime.CompilerServices;
using GHIElectronics.TinyCLR.Devices.Network;

namespace GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0 {
    public class Winc15x0Interface {
        public NetworkController NetworkController { get; }

        private bool initialized = false;

        public Winc15x0Interface() => this.NetworkController = NetworkController.FromName("GHIElectronics.TinyCLR.NativeApis.ATWINC15xx.NetworkController");

        ~Winc15x0Interface() => this.Dispose();

        public void Dispose() {
            this.NetworkController.Dispose();
            GC.SuppressFinalize(this);
        }

        public string GetFirmwareVersion() {
            this.TurnOn();

            this.NativeReadFirmwareVersion(out var ver1, out var ver2);

            var major = (ver1 >> 16) & 0xFF;
            var minor = (ver1 >> 8) & 0xFF;
            var patch = (ver1 >> 0) & 0xFF;

            return $"{major}.{minor}.{patch}.{ver2}";
        }

        public bool FirmwareUpdate(string url, int timeout) {
            this.TurnOn();

            return this.NativeFirmwareUpdatebyOta(url, timeout);
        }

        public bool FirmwareUpdate(byte[] buffer) => this.FirmwareUpdate(buffer, 0, buffer.Length);

        public bool FirmwareUpdate(byte[] buffer, int offset, int count) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(buffer));

            this.TurnOn();

            return this.NativeFirmwareUpdate(buffer, offset, count);
        }

        private void TurnOn() {
            if (!this.initialized) {
                if (this.NativeTurnOn()) {
                    this.initialized = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeTurnOn();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeFirmwareUpdatebyOta(string url, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeFirmwareUpdate(byte[] data, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeReadFirmwareVersion(out uint ver1, out uint ver2);
    }
}
