using System;
using System.Runtime.CompilerServices;
using GHIElectronics.TinyCLR.Devices.Network;

namespace GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0 {
    public class Winc15x0Interface {
        public NetworkController Controller { get; }

        public Winc15x0Interface() => this.Controller = NetworkController.FromName("GHIElectronics.TinyCLR.NativeApis.ATWINC15xx.NetworkController");

        ~Winc15x0Interface() => this.Dispose();

        public void Dispose() {
            this.Controller.Dispose();
            GC.SuppressFinalize(this);
        }

        public string GetFirmwareVersion() {
            this.NativeReadFirmwareVersion(out var ver1, out var ver2);

            var major = (ver1 >> 16) & 0xFF;
            var monor = (ver1 >> 8) & 0xFF;
            var path = (ver1 >> 0) & 0xFF;

            return major.ToString() + "." + monor.ToString() + "." + path.ToString() + " Svnrev " + ver2.ToString();
        }

        public bool TurnOn() => this.NativeTurnOn();
        public uint GetFlashSize() => this.NativeGetFlashSize();
        public uint ReadChipId() => this.NativeReadChipId();
        public void ReadFirmwareVersion(out uint ver1, out uint ver2) => this.ReadFirmwareVersion(out ver1, out ver2);
        public bool FirmwareUpdatebyOta(string url, int timeout) => this.NativeFirmwareUpdatebyOta(url, timeout);
        public bool FirmwareUpdate(byte[] data, int offset, int count) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > data.Length) throw new ArgumentOutOfRangeException(nameof(data));

            return this.NativeFirmwareUpdate(data, offset, count);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeTurnOn();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeFirmwareUpdatebyOta(string url, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeFirmwareUpdate(byte[] data, int offset, int count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern uint NativeGetFlashSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern uint NativeReadChipId();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeReadFirmwareVersion(out uint ver1, out uint ver2);
    }
}
