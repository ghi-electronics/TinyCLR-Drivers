using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInterface;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Net.NetworkInterface;

namespace GHIElectronics.TinyCLR.Drivers.Temp.Ethernet {
    public class EthernetInterface : NetworkInterface, ISocketProvider, ISslStreamProvider, IDnsProvider, IDisposable {
        private readonly Hashtable netifSockets;

        GpioPin ResetPin;

        public EthernetInterface(int reset) {

            this.netifSockets = new Hashtable();

            if (this.Initialize())
                NetworkInterface.RegisterNetworkInterface(this);

            this.ResetPin = GpioController.GetDefault().OpenPin(reset);

            this.ResetPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        ~EthernetInterface() => this.Dispose(false);

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                NetworkInterface.DeregisterNetworkInterface(this);
            }
        }

        public void Reset() {
            this.ResetPin.Write(GpioPinValue.Low);
            System.Threading.Thread.Sleep(250);
            this.ResetPin.Write(GpioPinValue.High);
            System.Threading.Thread.Sleep(100);
        }
        int ISocketProvider.Accept(int socket) => throw new NotImplementedException();

        int ISslStreamProvider.AuthenticateAsClient(int socketHandle, string targetHost, X509Certificate certificate, SslProtocols[] sslProtocols) => socketHandle;

        int ISslStreamProvider.AuthenticateAsServer(int socketHandle, X509Certificate certificate, SslProtocols[] sslProtocols) => throw new NotImplementedException();

        int ISslStreamProvider.Available(int handle) => ((ISocketProvider)this).Available(handle);

        void ISslStreamProvider.Close(int handle) => ((ISocketProvider)this).Close(handle);

        int ISocketProvider.Available(int socket) => this.ISocketProviderNativeAvailable(socket);

        void ISocketProvider.Bind(int socket, SocketAddress address) => throw new NotImplementedException();

        void ISocketProvider.Close(int socket) {
            this.ISocketProviderNativeClose(socket);

            this.netifSockets.Remove(socket);
        }

        void ISocketProvider.Connect(int socket, SocketAddress address) {
            if (!this.netifSockets.Contains(socket)) throw new ArgumentException();
            if (address.Family != AddressFamily.InterNetwork) throw new ArgumentException();

            if (this.ISocketProviderNativeConnect(socket, address) == true) {
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

        int ISslStreamProvider.Read(int handle, byte[] buffer, int offset, int count, int timeout) => ((ISocketProvider)this).Receive(handle, buffer, offset, count, SocketFlags.None, timeout);

        int ISocketProvider.Receive(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout) => this.ISocketProviderNativeReceive(socket, buffer, offset, count, flags, timeout);

        int ISocketProvider.ReceiveFrom(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, ref SocketAddress address) => throw new NotImplementedException();

        int ISocketProvider.Send(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout) => this.ISocketProviderNativeSend(socket, buffer, offset, count, flags, timeout);

        int ISocketProvider.SendTo(int socket, byte[] buffer, int offset, int count, SocketFlags flags, int timeout, SocketAddress address) => throw new NotImplementedException();

        void ISocketProvider.SetOption(int socket, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) => this.ISocketProviderNativeSetOption(socket, optionLevel, optionName, optionValue);

        int ISslStreamProvider.Write(int handle, byte[] buffer, int offset, int count, int timeout) => ((ISocketProvider)this).Send(handle, buffer, offset, count, SocketFlags.None, timeout);

        void IDnsProvider.GetHostByName(string name, out string canonicalName, out SocketAddress[] addresses) {

            this.IDnsProviderNativeGetHostByName(name, out var ipAddress);

            canonicalName = "";

            addresses = new[] { new IPEndPoint(ipAddress, 443).Serialize() };
        }

        // override
        public override PhysicalAddress GetPhysicalAddress() {
            this.NativeGetPhysicalAddress(out var ip);

            if (ip == null) ip = new byte[] { 0, 0, 0, 0 };

            return new PhysicalAddress(ip);
        }

        public override string Id => nameof(Ethernet);
        public override string Name => this.Id;
        public override string Description => string.Empty;
        public override OperationalStatus OperationalStatus => throw new NotImplementedException();
        public override bool IsReceiveOnly => false;
        public override bool SupportsMulticast => false;
        public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Wireless80211;

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent) => networkInterfaceComponent == NetworkInterfaceComponent.IPv4;

        //Native
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool Initialize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool Open();

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
        private extern bool ISocketProviderNativeConnect(int socket, SocketAddress address);

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
