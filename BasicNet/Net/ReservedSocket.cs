using System;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {
    internal sealed class ReservedSocket : Socket {
        public ReservedSocket(INetworkInterface netif, ProtocolType protocolType)
            : this(netif, AddressFamily.InterNetwork, SocketType.Dgram, protocolType) {
        }

        public ReservedSocket(INetworkInterface netif, AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(netif, addressFamily, socketType, protocolType, true) {
        }
    }
}
