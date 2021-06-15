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
        IPAddress Address { get; set; }
        IPAddress SubnetMask  { get; set; }
        IPAddress GatewayAddress { get; set; }
        IPAddress PrimaryDnsServer { get; set; }
        IPAddress SecondaryDnsServer { get; set; }
        ISocketImpl CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool useReservedSocket);
        void SetInterfaceSettings(NetworkInterfaceSettings networkInterfaceSettings);
        void Enable();
    }

    public class NetworkInterfaceSettings {
        public IPAddress Address { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress GatewayAddress { get; set; }
        public IPAddress[] DnsAddresses { get; set; }
        public byte[] MacAddress { get; set; }
        public bool DhcpEnable { get; set; }
        public bool DynamicDnsEnable { get; set; }
        public string Hostname { get; set; } = null;
    }
}
