using System;

using GHIElectronics.TinyCLR.Drivers.BasicNet;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;

namespace GHIElectronics.TinyCLR.Drivers.WIZnet.W5500 {
    internal class SocketImpl : ISocketImpl {
        private readonly W5500Controller parent;
        private int slot;
        private AddressFamily addressFamily;
        private SocketType socketType;
        private ProtocolType protocolType = ProtocolType.Unknown;
        private int sendTimeout = -1;
        private int recvTimeout = -1;
        private EndPoint localEP;

        public SocketImpl(W5500Controller parent, int slot, AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) {
            this.parent = parent;
            this.slot = slot;
            this.addressFamily = addressFamily;
            this.socketType = socketType;
            this.protocolType = protocolType;
        }

        internal int SocketNumber => this.slot;

        public EndPoint LocalEndPoint {
            get {
                this.localEP = this.parent.GetLocalEndpoint(this.slot);
                return this.localEP;
            }
        }

        public EndPoint RemoteEndPoint => this.parent.GetRemoteEndpoint(this.slot);

        public int ReceiveTimeout {
            get => this.recvTimeout;
            set => this.recvTimeout = value;
        }

        public int SendTimeout {
            get => this.sendTimeout;
            set => this.sendTimeout = value;
        }

        public void Bind(EndPoint localEP) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            this.localEP = localEP;
            this.parent.Bind(this.slot, localEP);
        }

        public void Connect(EndPoint remoteEP) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            this.parent.Connect(this.slot, ref this.localEP, remoteEP);
        }

        public void Listen() {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            this.parent.Listen(this.slot);
        }

        public ISocketImpl Accept() {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Accept(this.slot, this.protocolType);
        }

        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Send(this.slot, buffer, offset, size, socketFlags, -1);
        }

        public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.SendTo(this.slot, buffer, offset, size, socketFlags, -1, ref this.localEP, remoteEP);
        }

        public byte[] Receive(SocketFlags socketFlags) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Receive(this.slot, socketFlags, -1);
        }

        public byte[] Receive(SocketFlags socketFlags, int timeout) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Receive(this.slot, socketFlags, timeout);
        }

        public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Receive(this.slot, buffer, offset, size, socketFlags, -1);
        }

        public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeout) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.Receive(this.slot, buffer, offset, size, socketFlags, timeout);
        }

        public byte[] ReceiveFrom(SocketFlags socketFlags, ref EndPoint remoteEP) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.ReceiveFrom(this.slot, socketFlags, -1, ref remoteEP);
        }

        public byte[] ReceiveFrom(SocketFlags socketFlags, ref EndPoint remoteEP, int timeout) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.ReceiveFrom(this.slot, socketFlags, timeout, ref remoteEP);
        }

        public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.ReceiveFrom(this.slot, buffer, offset, size, socketFlags, -1, ref remoteEP);
        }

        public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, int timeout) {
            if (this.slot == -1)
                throw new ObjectDisposedException("SocketImpl");
            return this.parent.ReceiveFrom(this.slot, buffer, offset, size, socketFlags, timeout, ref remoteEP);
        }

        public int BytesAvailable {
            get {
                if (this.slot == -1)
                    throw new ObjectDisposedException("SocketImpl");
                return this.parent.GetBytesAvailable(this.slot);
            }
        }

        public void Close() =>
            // no check for object disposed - close on disposed object is harmless
            this.Dispose();

        public void Dispose() {
            if (this.slot != -1) {
                this.parent.Close(this.slot);
                this.parent.ReleaseSocket(this.slot);
                this.slot = -1;
            }
        }
    }
}
