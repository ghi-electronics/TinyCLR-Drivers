using System;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;
using System.Text;
using System.Threading;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    public class DhcpClient {
        private const int DHCP_FLAGSBROADCAST = 0x8000;
        private const int DHCP_SERVER_PORT = 67;
        private const int DHCP_CLIENT_PORT = 68;
        private const int DHCP_HTYPE10MB = 1;
        private const int DHCP_HLENETHERNET = 6;
        private const int DHCP_HOPS = 0;
        private const int DHCP_SECS = 0;
        private const int DhcpMsgIndex_Op = 0;
        private const int DhcpMsgIndex_xid = 4;
        private const int DhcpMsgIndex_chaddr = 28;
        private const int DhcpMsgIndex_yiaddr = 16;

        private static byte[] magicCookie = new byte[4] { 0x63, 0x82, 0x53, 0x63 };

        public delegate void DhcpAddressChangedEventHandler();
        public event DhcpAddressChangedEventHandler OnAddressChangedEventHandler;

        internal enum OPCode : byte {
            BOOTREQUEST = 1,
            BOOTREPLY = 2,
        }

        internal enum MessageType : byte {
            UNKNOWN = 0,
            DISCOVER = 1,
            OFFER = 2,
            REQUEST = 3,
            DECLINE = 4,
            ACK = 5,
            NAK = 6,
            RELEASE = 7,
            INFORM = 8,
        }

        internal enum DhcpState : byte {
            DISCOVER = 1,
            REQUEST = 2,
            LEASED = 3,
            REREQUEST = 4,
            RELEASE = 5,
        }

        internal enum Options {
            padOption = 0,
            subnetMask = 1,
            timerOffset = 2,
            routersOnSubnet = 3,
            timeServer = 4,
            nameServer = 5,
            dns = 6,
            logServer = 7,
            cookieServer = 8,
            lprServer = 9,
            impressServer = 10,
            resourceLocationServer = 11,
            hostName = 12,
            bootFileSize = 13,
            meritDumpFile = 14,
            domainName = 15,
            swapServer = 16,
            rootPath = 17,
            extentionsPath = 18,
            IPforwarding = 19,
            nonLocalSourceRouting = 20,
            policyFilter = 21,
            maxDgramReasmSize = 22,
            defaultIPTTL = 23,
            pathMTUagingTimeout = 24,
            pathMTUplateauTable = 25,
            ifMTU = 26,
            allSubnetsLocal = 27,
            broadcastAddr = 28,
            performMaskDiscovery = 29,
            maskSupplier = 30,
            performRouterDiscovery = 31,
            routerSolicitationAddr = 32,
            staticRoute = 33,
            trailerEncapsulation = 34,
            arpCacheTimeout = 35,
            ethernetEncapsulation = 36,
            tcpDefaultTTL = 37,
            tcpKeepaliveInterval = 38,
            tcpKeepaliveGarbage = 39,
            nisDomainName = 40,
            nisServers = 41,
            ntpServers = 42,
            vendorSpecificInfo = 43,
            netBIOSnameServer = 44,
            netBIOSdgramDistServer = 45,
            netBIOSnodeType = 46,
            netBIOSscope = 47,
            xFontServer = 48,
            xDisplayManager = 49,
            dhcpRequestedIPaddr = 50,
            dhcpIPaddrLeaseTime = 51,
            dhcpOptionOverload = 52,
            dhcpMessageType = 53,
            dhcpServerIdentifier = 54,
            dhcpParamRequest = 55,
            dhcpMsg = 56,
            dhcpMaxMsgSize = 57,
            dhcpRenewalTime = 58,
            dhcpRebindingTime = 59,
            dhcpClassIdentifier = 60,
            dhcpClientIdentifier = 61,
            endOption = 255
        }

        private byte[] hostname;
        private IPAddress dhcpServerIPAddr = new IPAddress();
        private IPAddress assignedIPAddr = new IPAddress();
        private IPAddress subnetMask = new IPAddress();
        private IPAddress gwIPAddr = new IPAddress();
        private IPAddress dnsIPAddr = new IPAddress();

        static DhcpState state = DhcpState.DISCOVER;
        private uint transactionId;
        private readonly Random random = new Random();

        private bool haveAddress;
        private ManualResetEvent addressObtainedEvent = new ManualResetEvent(false);

        private readonly INetworkInterface netif;
        private Socket socket;

        public DhcpClient(INetworkInterface netif)
            : this(netif, null) {
        }

        public DhcpClient(INetworkInterface netif, string hostname) {
            this.netif = netif;
            if (hostname != null && hostname.Length > 0) {
                if (hostname.Length > 250)
                    throw new ArgumentException("Hostname is too long");
                this.hostname = Encoding.UTF8.GetBytes(hostname);
            }

            this.dhcpServerIPAddr = new IPAddress();
            this.assignedIPAddr = new IPAddress();
            this.subnetMask = new IPAddress();
            this.gwIPAddr = new IPAddress();
            this.dnsIPAddr = new IPAddress();

            this.netif.Address = this.assignedIPAddr;
            this.netif.SubnetMask = this.subnetMask;
            this.netif.GatewayAddress = this.gwIPAddr;
            this.netif.PrimaryDnsServer = null;
            this.netif.SecondaryDnsServer = null;

            new Thread(this.RequestLease).Start();
        }

        public bool HaveAddress => this.haveAddress;

        public WaitHandle AddressObtainedEvent => this.addressObtainedEvent;

        private void RequestLease() {
            EndPoint ep = null;

            this.socket = new ReservedSocket(this.netif, ProtocolType.Udp) {
                ReceiveTimeout = 5000
            };
            this.socket.Bind(new IPEndPoint(0, DHCP_CLIENT_PORT));

            var now = DateTime.UtcNow;
            var retries = 5;
            var delay = 100;

            this.SendDhcpDiscover();

            while (state != DhcpState.LEASED) {
                var buffer = this.socket.ReceiveFrom(ref ep);
                var receivedLen = buffer.Length;

                if (((IPEndPoint)ep).Port != DHCP_SERVER_PORT)
                    continue;

                var msgType = this.ParseDhcpMessage(buffer);

                switch (state) {
                    case DhcpState.DISCOVER:
                        if (msgType == MessageType.OFFER) {
                            this.SendDhcpRequest();
                        }
                        break;
                    case DhcpState.REQUEST:
                        if (msgType == MessageType.ACK) {
                            this.netif.Address = this.assignedIPAddr;
                            this.netif.SubnetMask = this.subnetMask;
                            this.netif.GatewayAddress = this.gwIPAddr;
                            this.netif.PrimaryDnsServer = this.dnsIPAddr;

                            this.socket.Close();
                            this.socket = null;
                            this.SendArpMessage();
                            state = DhcpState.LEASED;
                            this.haveAddress = true;
                            this.addressObtainedEvent.Set();
                            this.OnAddressChangedEventHandler?.Invoke();
                        }
                        else {
                            state = DhcpState.DISCOVER;
                        }
                        break;
                    case DhcpState.REREQUEST:
                        state = DhcpState.DISCOVER;
                        break;
                    case DhcpState.RELEASE:
                        state = DhcpState.DISCOVER;
                        break;
                    default:
                        break;
                }

                if (state == DhcpState.DISCOVER) {
                    if (--retries == 0) {
                        //TODO: record an error status
                        break;
                    }
                    Thread.Sleep(delay);
                    // use a progressive back-off
                    delay *= 2;
                    this.SendDhcpDiscover();
                }
            }
            //TODO: Schedule a callback for renewal
        }

        private MessageType ParseDhcpMessage(byte[] inBuffer) {
            var addr = new byte[4];
            var p = 0;
            var e = 0;
            var opt_len = 0;
            var type = MessageType.UNKNOWN;
            if (inBuffer[DhcpMsgIndex_Op] == (byte)OPCode.BOOTREPLY) {
                if (!CompareByteArrays(this.netif.PhysicalAddress, 0, inBuffer, DhcpMsgIndex_chaddr, 6) && !CheckTransactionId(inBuffer, this.transactionId)) {
                    //Debug.Print("Transaction id mismatch");
                }
                else {
                    Array.Copy(inBuffer, DhcpMsgIndex_yiaddr, addr, 0, 4);
                    this.assignedIPAddr = new IPAddress(addr);

                    type = MessageType.UNKNOWN;
                    p = 240; // Options Index
                    e = p + (inBuffer.Length - 240);
                    while (p < e) {
                        switch ((Options)inBuffer[p++]) {
                            case Options.endOption:
                                return type;
                            case Options.padOption:
                                break;
                            case Options.dhcpMessageType:
                                opt_len = inBuffer[p++];
                                type = (MessageType)inBuffer[p];
                                break;
                            case Options.subnetMask:
                                opt_len = inBuffer[p++];
                                Array.Copy(inBuffer, p, addr, 0, 4);
                                this.subnetMask = new IPAddress(addr);
                                break;
                            case Options.routersOnSubnet:
                                opt_len = inBuffer[p++];
                                Array.Copy(inBuffer, p, addr, 0, 4);
                                this.gwIPAddr = new IPAddress(addr);
                                break;
                            case Options.dns:
                                opt_len = inBuffer[p++];
                                Array.Copy(inBuffer, p, addr, 0, 4);
                                this.dnsIPAddr = new IPAddress(addr);
                                break;
                            case Options.dhcpServerIdentifier:
                                opt_len = inBuffer[p++];
                                Array.Copy(inBuffer, p, addr, 0, 4);
                                this.dhcpServerIPAddr = new IPAddress(addr);
                                break;
                            default:
                                opt_len = inBuffer[p++];
                                break;
                        }
                        p += opt_len;
                    }
                }
            }
            return MessageType.UNKNOWN;
        }

        private void SendDhcpRequest() {
            state = DhcpState.REQUEST;
            this.transactionId = (uint)this.random.Next(int.MaxValue);

            var buffer = new byte[548];
            var i = 0;
            buffer[i++] = (byte)OPCode.BOOTREQUEST;
            buffer[i++] = DHCP_HTYPE10MB;
            buffer[i++] = 6;// Hardware address length.
            buffer[i++] = DHCP_HOPS;
            // Insert transaction id (big endian)
            buffer[i++] = (byte)(this.transactionId >> 24);
            buffer[i++] = (byte)(this.transactionId >> 16);
            buffer[i++] = (byte)(this.transactionId >> 8);
            buffer[i++] = (byte)(this.transactionId);
            // Seconds passed since client began the request process USHORT
            buffer[i++] = 0x00;
            buffer[i++] = 0x00;
            // Broadcast flag
            var addr = this.assignedIPAddr.GetAddressBytes();
            if (state < DhcpState.LEASED) {
                buffer[i++] = 0x00;
                buffer[i++] = 0x00;
                // Client IP addess 0.0.0.0 (the existing one)
                i += 4;
            }
            else {
                buffer[i++] = 0x00;
                buffer[i++] = 0x00;
                // Client IP addess 0.0.0.0 (the existing one)
                buffer[i++] = addr[0];
                buffer[i++] = addr[1];
                buffer[i++] = addr[2];
                buffer[i++] = addr[3];
            }

            // Your Client IP addess 0.0.0.0 (the one provided by the server)
            i += 4;
            // DHCP server IP addess 0.0.0.0
            i += 4;
            // DHCP Relay Agent IP addess 0.0.0.0
            i += 4;
            // Mac address
            var mac = this.netif.PhysicalAddress;
            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];
            // 10 padding byte 
            i += 10;

            // Server Name 128 bytes
            i += 64;
            // Boot File name
            i += 128;

            // Magic Cookie
            buffer[i++] = magicCookie[0];
            buffer[i++] = magicCookie[1];
            buffer[i++] = magicCookie[2];
            buffer[i++] = magicCookie[3];

            /* Option Request Param. */
            buffer[i++] = (byte)Options.dhcpMessageType;
            buffer[i++] = 0x01;
            buffer[i++] = (byte)MessageType.REQUEST;

            // Client identifier
            buffer[i++] = (byte)Options.dhcpClientIdentifier;
            buffer[i++] = 0x07;
            buffer[i++] = 0x01;
            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];
            if (state < DhcpState.LEASED) {
                buffer[i++] = (byte)Options.dhcpRequestedIPaddr;
                buffer[i++] = 0x04;
                buffer[i++] = addr[0];
                buffer[i++] = addr[1];
                buffer[i++] = addr[2];
                buffer[i++] = addr[3];

                buffer[i++] = (byte)Options.dhcpServerIdentifier;
                buffer[i++] = 0x04;
                var dhcpServerAddr = this.dhcpServerIPAddr.GetAddressBytes();
                buffer[i++] = dhcpServerAddr[0];
                buffer[i++] = dhcpServerAddr[1];
                buffer[i++] = dhcpServerAddr[2];
                buffer[i++] = dhcpServerAddr[3];
            }

            if (this.hostname != null && this.hostname.Length > 0) {
                buffer[i++] = (byte)Options.hostName;
                buffer[i++] = (byte)(this.hostname.Length);
                Array.Copy(this.hostname, 0, buffer, i, this.hostname.Length);
                i += this.hostname.Length;
            }


            buffer[i++] = (byte)Options.dhcpParamRequest;
            buffer[i++] = 0x08;
            buffer[i++] = (byte)Options.subnetMask;
            buffer[i++] = (byte)Options.routersOnSubnet;
            buffer[i++] = (byte)Options.dns;
            buffer[i++] = (byte)Options.domainName;
            buffer[i++] = (byte)Options.dhcpRenewalTime;
            buffer[i++] = (byte)Options.dhcpRebindingTime;
            buffer[i++] = (byte)Options.performRouterDiscovery;
            buffer[i++] = (byte)Options.staticRoute;
            buffer[i++] = (byte)Options.endOption;

            if (state < DhcpState.LEASED) {
                if (this.socket.SendTo(buffer, new IPEndPoint(0xFFFFFFFF, DHCP_SERVER_PORT)) != buffer.Length) {
                    throw new Exception("Partial send");
                }
            }
            else {
                if (this.socket.SendTo(buffer, new IPEndPoint(this.dhcpServerIPAddr, DHCP_SERVER_PORT)) != buffer.Length) {
                    throw new Exception("Partial send");
                }
            }
        }

        private void SendArpMessage() {
            var buffer = new byte[42];
            var i = 0;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;

            var mac = this.netif.PhysicalAddress;
            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];

            buffer[i++] = 0x08; //ARP
            buffer[i++] = 0x06;// ARP

            buffer[i++] = 0x00;
            buffer[i++] = 0x01;
            buffer[i++] = 0x08;
            buffer[i++] = 0x00;
            buffer[i++] = 0x06;
            buffer[i++] = 0x04;
            buffer[i++] = 0x00;
            buffer[i++] = 0x01;

            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];

            var addr = this.assignedIPAddr.GetAddressBytes();
            buffer[i++] = addr[0];
            buffer[i++] = addr[1];
            buffer[i++] = addr[2];
            buffer[i++] = addr[3];
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = 0xff;
            buffer[i++] = addr[0];
            buffer[i++] = addr[1];
            buffer[i++] = addr[2];
            buffer[i++] = addr[3];
            // Don't use ReservedSocket here. ProtocolType.Raw requires socket slot 0 on the W5500 and ReservedSocket uses the last socket slot.
            // This is by-design since we don't want reserved sockets to interfere with MACRAW operation if not absolutely necessary
            var arpSocket = new Socket(this.netif, ProtocolType.Raw);
            arpSocket.SendTo(buffer, new IPEndPoint(new IPAddress(new byte[] { 255, 255, 255, 255 }), 5000));
            arpSocket.Close();
        }

        private void SendDhcpDiscover() {
            var buffer = new byte[548];

            state = DhcpState.DISCOVER;
            this.transactionId = (uint)this.random.Next(int.MaxValue);

            var i = 0;
            buffer[i++] = (byte)OPCode.BOOTREQUEST;
            buffer[i++] = DHCP_HTYPE10MB;
            buffer[i++] = 6;// Hardware address length.
            buffer[i++] = DHCP_HOPS;
            // Insert Xid (big endian)
            buffer[i++] = (byte)(this.transactionId >> 24);
            buffer[i++] = (byte)(this.transactionId >> 16);
            buffer[i++] = (byte)(this.transactionId >> 8);
            buffer[i++] = (byte)(this.transactionId);
            // Seconds passed since client began the request process USHORT
            buffer[i++] = 0x00;
            buffer[i++] = 0x00;
            // Broadcast flag
            buffer[i++] = 0x80;
            buffer[i++] = 0x00;
            // Client IP addess 0.0.0.0 (the existing one)
            i += 4;
            // Your Client IP addess 0.0.0.0 (the one provided by the server)
            i += 4;
            // DHCP server IP addess 0.0.0.0
            i += 4;
            // DHCP Relay Agent IP addess 0.0.0.0
            i += 4;
            // Mac address
            var mac = this.netif.PhysicalAddress;
            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];
            // 10 padding byte 
            i += 10;

            // Server Name 128 bytes
            i += 64;
            // Boot File name
            i += 128;

            // Magic Cookie
            buffer[i++] = magicCookie[0];
            buffer[i++] = magicCookie[1];
            buffer[i++] = magicCookie[2];
            buffer[i++] = magicCookie[3];

            /* Option Request Param. */
            buffer[i++] = (byte)Options.dhcpMessageType;
            buffer[i++] = 0x01;
            buffer[i++] = (byte)MessageType.DISCOVER;

            // Client identifier
            buffer[i++] = (byte)Options.dhcpClientIdentifier;
            buffer[i++] = 0x07;
            buffer[i++] = 0x01;
            buffer[i++] = mac[0];
            buffer[i++] = mac[1];
            buffer[i++] = mac[2];
            buffer[i++] = mac[3];
            buffer[i++] = mac[4];
            buffer[i++] = mac[5];

            // host name
            if (this.hostname != null && this.hostname.Length > 0) {
                buffer[i++] = (byte)Options.hostName;
                buffer[i++] = (byte)(this.hostname.Length);
                Array.Copy(this.hostname, 0, buffer, i, this.hostname.Length);
                i += this.hostname.Length;
            }

            buffer[i++] = (byte)Options.dhcpParamRequest;
            buffer[i++] = 0x06;
            buffer[i++] = (byte)Options.subnetMask;
            buffer[i++] = (byte)Options.routersOnSubnet;
            buffer[i++] = (byte)Options.dns;
            buffer[i++] = (byte)Options.domainName;
            buffer[i++] = (byte)Options.dhcpRenewalTime;
            buffer[i++] = (byte)Options.dhcpRebindingTime;
            buffer[i++] = (byte)Options.endOption;

            if (this.socket.SendTo(buffer, new IPEndPoint(0xFFFFFFFF, DHCP_SERVER_PORT)) != buffer.Length) {
                throw new Exception("Discover Message was sent partially");
            }
        }

        private static bool CompareByteArrays(byte[] array1, int index1, byte[] array2, int index2, int size) {
            for (var i = 0; i < size; i++) {
                if (array1[index1 + i] != array2[index2 + i])
                    return false;
            }
            return true;
        }

        private static bool CheckTransactionId(byte[] dhcpMsg, uint transactionId) {
            var total = 0;
            total += (dhcpMsg[DhcpMsgIndex_xid] - (byte)(transactionId >> 24));
            total += (dhcpMsg[DhcpMsgIndex_xid + 1] - (byte)(transactionId >> 16));
            total += (dhcpMsg[DhcpMsgIndex_xid + 2] - (byte)(transactionId >> 8));
            total += (dhcpMsg[DhcpMsgIndex_xid + 2] - (byte)(transactionId));
            if (total == 0)
                return true;
            return false;
        }

    }
}
