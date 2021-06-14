using System;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {
    public class Socket : IDisposable {
        private readonly ProtocolType protocolType;
        private ISocketImpl socket;

        public Socket(INetworkInterface netif, AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : this(netif, addressFamily, socketType, protocolType, false) {
        }

        public Socket(INetworkInterface netif, ProtocolType protocolType)
            : this(netif, AddressFamily.InterNetwork, SocketType.Dgram, protocolType) {
        }

        protected Socket(INetworkInterface netif, AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool useReservedSocket) {
            this.protocolType = protocolType;
            this.socket = netif.CreateSocket(addressFamily, socketType, protocolType, useReservedSocket);
        }

        private Socket(ISocketImpl socketImpl) => this.socket = socketImpl;//this.protocolType = socketImpl.ProtocolType;

        public EndPoint LocalEndPoint => this.socket.LocalEndPoint;
        public EndPoint RemoteEndPoint => this.socket.RemoteEndPoint;
        public int ReceiveTimeout { get => this.socket.ReceiveTimeout; set => this.socket.ReceiveTimeout = value; }
        public int SendTimeout { get => this.socket.SendTimeout; set => this.socket.SendTimeout = value; }
        public void Bind(EndPoint localEP) => this.socket.Bind(localEP);
        public void Connect(EndPoint remoteEP) => this.socket.Connect(remoteEP);
        public void Close() => this.socket.Close();
        public void Listen() => this.socket.Listen();
        public Socket Accept() {
            var socketImpl = this.socket.Accept();
            return new Socket(socketImpl);
        }

        public int Send(byte[] buffer, int size, SocketFlags socketFlags) => this.socket.Send(buffer, 0, size, socketFlags);
        public int Send(byte[] buffer, SocketFlags socketFlags) => this.socket.Send(buffer, 0, buffer.Length, socketFlags);
        public int Send(byte[] buffer) => this.socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags) => this.socket.Send(buffer, offset, size, socketFlags);
        public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP) => this.socket.SendTo(buffer, 0, size, socketFlags, remoteEP);
        public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP) => this.socket.SendTo(buffer, 0, buffer.Length, socketFlags, remoteEP);
        public int SendTo(byte[] buffer, EndPoint remoteEP) => this.socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, remoteEP);
        public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP) => this.socket.SendTo(buffer, offset, size, socketFlags, remoteEP);

        public byte[] Receive(SocketFlags socketFlags) => this.socket.Receive(socketFlags);
        public int Receive(byte[] buffer, int size, SocketFlags socketFlags) => this.socket.Receive(buffer, 0, size, socketFlags);
        public int Receive(byte[] buffer, SocketFlags socketFlags) => this.socket.Receive(buffer, 0, buffer.Length, socketFlags);
        public int Receive(byte[] buffer) => this.socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
        public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags) => this.socket.Receive(buffer, offset, size, socketFlags);
        public byte[] ReceiveFrom(ref EndPoint remoteEP) => this.socket.ReceiveFrom(SocketFlags.None, ref remoteEP);
        public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP) => this.socket.ReceiveFrom(buffer, 0, size, socketFlags, ref remoteEP);
        public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP) => this.socket.ReceiveFrom(buffer, 0, buffer.Length, socketFlags, ref remoteEP);
        public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP) => this.socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP);
        public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP) => this.socket.ReceiveFrom(buffer, offset, size, socketFlags, ref remoteEP);

        public int BytesAvailable => this.socket.BytesAvailable;

        protected virtual void Dispose(bool disposing) {
            if (this.socket != null) {
                this.socket.Dispose();
                this.socket = null;
            }
            else
                throw new ObjectDisposedException("Socket");
        }

        void IDisposable.Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Socket() {
            this.Dispose(false);
        }
    }
}
