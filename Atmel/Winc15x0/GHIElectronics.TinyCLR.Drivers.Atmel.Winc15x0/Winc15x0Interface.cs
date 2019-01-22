using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInterface;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Net.NetworkInterface;

namespace GHIElectronics.TinyCLR.Drivers.Atmel.Winc15x0 {
    public class Winc15x0Interface : NetworkInterface, ISocketProvider, ISslStreamProvider, IDnsProvider, IDisposable {
        private readonly Hashtable netifSockets;

        public Winc15x0Interface(SpiController spiController, int chipSelect, int interrupt, int enable, int reset, int clockRate) {

            this.netifSockets = new Hashtable();

            if (this.Initialize(spiController, chipSelect, interrupt, enable, reset, clockRate))
                NetworkInterface.RegisterNetworkInterface(this);
        }

        ~Winc15x0Interface() => this.Dispose(false);

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                NetworkInterface.DeregisterNetworkInterface(this);
            }
        }

        public string[] Scan() {
            var response = this.NativeScan(out var numAp);

            // 44 bytes for each AP - refer to native tstrM2mWifiscanResult
            var ssids = new string[numAp];

            for (var i = 0; i < numAp; i++) {
                var ssid = new char[32];

                var index = 0;
                for (var index2 = i * 44 + 10; index < ssid.Length; index++) {
                    ssid[index] = (char)response[index2++];
                }

                ssids[i] = new string(ssid);
            }
            return ssids;

        }

        int ISocketProvider.Accept(int socket) => throw new NotImplementedException();

        int ISslStreamProvider.AuthenticateAsClient(int socketHandle, string targetHost, X509Certificate certificate, SslProtocols[] sslProtocols) => socketHandle;

        int ISslStreamProvider.AuthenticateAsServer(int socketHandle, X509Certificate certificate, SslProtocols[] sslProtocols) => throw new NotImplementedException();

        int ISslStreamProvider.Available(int handle) => throw new NotImplementedException();

        void ISslStreamProvider.Close(int handle) => throw new NotImplementedException();

        int ISocketProvider.Available(int socket) => this.ISocketProviderNativeAvailable(socket);

        void ISocketProvider.Bind(int socket, SocketAddress address) => throw new NotImplementedException();

        void ISocketProvider.Close(int socket) {
            this.ISocketProviderNativeClose(socket);

            this.netifSockets.Remove(socket);
        }

        void ISocketProvider.Connect(int socket, SocketAddress address) {
            if (!this.netifSockets.Contains(socket)) throw new ArgumentException();
            if (address.Family != AddressFamily.InterNetwork) throw new ArgumentException();

            var addressInBytes = new byte[address.Size];
            for (var i = 0; i < addressInBytes.Length; i++) {
                addressInBytes[i] = address[i];
            }

            if (this.ISocketProviderNativeConnect(socket, addressInBytes) == true) {
                this.netifSockets[socket] = socket;
            }
        }

        int ISocketProvider.Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) {
            var id = this.ISocketProviderNativeCreate(addressFamily, socketType, protocolType);

            if (id >= 0) {
                this.netifSockets.Add(id, 0);

                return id;
            }

            throw new SocketException(SocketError.TooManyOpenSockets);
        }

        void ISocketProvider.GetLocalAddress(int socket, out SocketAddress address) => address = new SocketAddress(AddressFamily.InterNetwork, 16);

        void ISocketProvider.GetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) {
            if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Type)
                Array.Copy(BitConverter.GetBytes((int)SocketType.Stream), optionValue, 4);
        }

        void ISocketProvider.GetRemoteAddress(int socket, out SocketAddress address) => address = new SocketAddress(AddressFamily.InterNetwork, 16);

        void ISocketProvider.Listen(int socket, int backlog) => throw new NotImplementedException();

        bool ISocketProvider.Poll(int socket, int microSeconds, SelectMode mode) => this.ISocketProviderNativePoll(socket, microSeconds, mode);

        int ISslStreamProvider.Read(int handle, byte[] buffer, int offset, int count, int timeout) => throw new NotImplementedException();

        int ISocketProvider.Receive(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout) => this.ISocketProviderNativeReceive(socket, buffer, offset, count, flags, timeout);

        int ISocketProvider.ReceiveFrom(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, ref SocketAddress address) => throw new NotImplementedException();

        int ISocketProvider.Send(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout) => this.ISocketProviderNativeSend(socket, buffer, offset, count, flags, timeout);

        int ISocketProvider.SendTo(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, SocketAddress address) => throw new NotImplementedException();

        void ISocketProvider.SetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) => this.ISocketProviderNativeSetOption(socket, optionLevel, optionName, optionValue);

        int ISslStreamProvider.Write(int handle, byte[] buffer, int offset, int count, int timeout) => throw new NotImplementedException();

        void IDnsProvider.GetHostByName(string name, out string canonicalName, out SocketAddress[] addresses) {

            this.IDnsProviderNativeGetHostByName(name, out var ipAddress);

            canonicalName = "";

            addresses = new[] { new IPEndPoint(ipAddress, 80).Serialize() };
        }

        // override
        public override PhysicalAddress GetPhysicalAddress() {
            this.NativeGetPhysicalAddress(out var ip);

            if (ip == null) ip = new byte[] { 0, 0, 0, 0 };

            return new PhysicalAddress(ip);
        }

        public override string Id => nameof(Winc15x0);
        public override string Name => this.Id;
        public override string Description => string.Empty;
        public override OperationalStatus OperationalStatus => throw new NotImplementedException();
        public override bool IsReceiveOnly => false;
        public override bool SupportsMulticast => false;
        public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Wireless80211;

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent) => networkInterfaceComponent == NetworkInterfaceComponent.IPv4;

        //Native
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool Initialize(SpiController spiController, int chipSelect, int interrupt, int enable, int reset, int clockRate);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool Reset();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool Open();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern byte[] NativeScan(out int numAp);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool JoinNetwork(string ssid, string pass);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeAccept(int socket);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISslStreamProviderNativeAuthenticateAsClient(int socketHandle, string targetHost, X509Certificate certificate, SslProtocols[] sslProtocols);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISslStreamProviderNativeAuthenticateAsServer(int socketHandle, X509Certificate certificate, SslProtocols[] sslProtocols);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISslStreamProviderNativeAvailable(int handle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISslStreamProviderNativeClose(int handle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeAvailable(int socket);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeBind(int socket, SocketAddress address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeClose(int socket);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool ISocketProviderNativeConnect(int socket, byte[] socketAddressInBytes);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeCreate(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeGetLocalAddress(int socket, out SocketAddress address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeGetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeGetRemoteAddress(int socket, out SocketAddress address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeListen(int socket, int backlog);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool ISocketProviderNativePoll(int socket, int microSeconds, SelectMode mode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISslStreamProviderNativeRead(int handle, byte[] buffer, int offset, int count, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeReceive(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeReceiveFrom(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, ref SocketAddress address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeSend(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISocketProviderNativeSendTo(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, SocketAddress address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void ISocketProviderNativeSetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int ISslStreamProviderNativeWrite(int handle, byte[] buffer, int offset, int count, int timeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void IDnsProviderNativeGetHostByName(string name, out long address);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeGetPhysicalAddress(out byte[] ip);
    }
}
