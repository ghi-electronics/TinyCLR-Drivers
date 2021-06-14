using GHIElectronics.TinyCLR.Drivers.BasicNet;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    /// <summary>
    /// This interface defines that basic properties and operations common to all network interfaces. It
    /// provides a single, common network interface abstraction for all intelligent hardware-networking-stack
    /// devices, like Wiznet and ESP8266.
    /// </summary>
    public interface INetworkInterface {
        byte[] PhysicalAddress { get; }

        IPAddress PrimaryDnsServer { get; }
        IPAddress SecondaryDnsServer { get; }

        ISocketImpl CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool useReservedSocket);

        void EnableDhcp();
        void EnableStaticIP(IPAddress addr, IPAddress subnet, IPAddress gateway, byte[] macAddr);
        void EnableStaticDns(IPAddress dns1);
        void EnableStaticDns(IPAddress dns1, IPAddress dns2);        
    }
}
