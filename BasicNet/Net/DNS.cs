using System;
using System.Text;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;
using System.Collections;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    public class Dns {
        private ushort transactionId;
        private readonly INetworkInterface netif;

        public enum QTYPE {
            A = 0x0001,
            NS = 0x0002,
            CNAME = 0x0005,
            SOA = 0x0006,
            WKS = 0x000B,
            PTR = 0x000C,
            MX = 0x000F,
            SRV = 0x0021,
            A6 = 0x0026,
            ANY = 0x00FF
        }

        public Dns(INetworkInterface netif) {
            this.netif = netif;
            this.transactionId = (ushort)(new Random().Next(ushort.MaxValue));
        }

        public IPHostEntry GetHostEntry(string hostNameOrAddress) {
            // if it's an ip address, then just convert it and return
            var ipaddress_helper = new byte[4];
            if (Inet_addr(hostNameOrAddress, ipaddress_helper)) {
                var ipHostEntry = new IPHostEntry {
                    hostName = hostNameOrAddress,
                    addressList = new IPAddress[1]
                };
                ipHostEntry.addressList[0] = new IPAddress(ipaddress_helper);
                return ipHostEntry;
            }

            // otherwise, we need to query the DNS server...

            if (this.netif.PrimaryDnsServer == null && this.netif.SecondaryDnsServer == null)
                throw new Exception("No DNS servers are configured.");

            var dnsServer = this.netif.PrimaryDnsServer;
            if (dnsServer == null)
                dnsServer = this.netif.SecondaryDnsServer;

            var dnsEndPoint = new IPEndPoint(dnsServer, 53);  //new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 236 }), 53);
            var dnsSocket = new ReservedSocket(this.netif, ProtocolType.Udp);

            // Send request to the server.
            IPHostEntry result = null;
            try {
                for (var loop = 0; loop < 3; loop++) {
                    var bytesToSend = this.MakeQuery(hostNameOrAddress);
                    var len = bytesToSend.Length;

                    dnsSocket.SendTo(bytesToSend, len, SocketFlags.None, dnsEndPoint);

                    EndPoint recEndPoint = null;
                    var buffer = dnsSocket.ReceiveFrom(ref recEndPoint);
                    if (!recEndPoint.Equals(dnsEndPoint))
                        continue;

                    try {
                        if (this.ParseResponse(buffer, out var answers)) {
                            var list = new ArrayList();
                            string name = null;
                            foreach (var answer in answers) {
                                if (answer is DnsARecord arec) {
                                    if (name == null)
                                        name = arec.Name;
                                    list.Add(arec);
                                }
                            }
                            if (list.Count > 0) {
                                result = new IPHostEntry {
                                    hostName = name,
                                    addressList = new IPAddress[list.Count]
                                };
                                for (var i = 0; i < list.Count; ++i) {
                                    result.addressList[i] = ((DnsARecord)list[i]).Address;
                                }
                                break;
                            }
                        }
                    }
                    catch {
                        // do another retry
                    }
                }
            }
            finally {
                dnsSocket.Close();
            }
            return result;
        }

        private byte[] MakeQuery(string hostname) {
            var hostnameAscii = Encoding.UTF8.GetBytes(hostname);

            var len = 12 + // header size
                hostnameAscii.Length + 2 + // hostname plus leading len plus trailing nul
                4; // QTYPE and QCLASS

            var buffer = new byte[len];

            var i = 0;

            // Insert transaction id (little endian)
            ++this.transactionId;
            buffer[i++] = (byte)(this.transactionId >> 8);
            buffer[i++] = (byte)(this.transactionId & 0xff);

            buffer[i++] = 0x01; // query, recursion requested
            buffer[i++] = 0x00;

            ushort qcount = 1;
            buffer[i++] = (byte)(qcount >> 8);
            buffer[i++] = (byte)(qcount & 0xff);

            // ancount
            buffer[i++] = 0;
            buffer[i++] = 0;

            // nscount
            buffer[i++] = 0;
            buffer[i++] = 0;

            // arcount
            buffer[i++] = 0;
            buffer[i++] = 0;

            // question section
            var ptr = 0;
            var count = 0;
            for (var src = 0; src < hostnameAscii.Length; ++src) {
                if (hostnameAscii[src] == '.') {
                    buffer[i + ptr] = (byte)count;
                    count = 0;
                    ptr = src + 1;
                }
                else {
                    buffer[i + src + 1] = hostnameAscii[src];
                    ++count;
                }
            }
            buffer[i + ptr] = (byte)count;
            i = i + hostnameAscii.Length + 1;
            buffer[i++] = 0;

            // QTYPE (Any)
            buffer[i++] = (byte)((ushort)QTYPE.A >> 8);
            buffer[i++] = (byte)((ushort)QTYPE.A & 0xff);

            // QCLASS (0x0001 == "IN")
            buffer[i++] = 0x00;
            buffer[i++] = 0x01;

            return buffer;
        }

        private bool ParseResponse(byte[] buffer, out DnsRecord[] answers) {
            answers = null;
            if (buffer.Length < 12)
                return false; // malformed packet - too short

            var tid = (ushort)(buffer[0] << 8 | buffer[1]);
            if (tid != this.transactionId)
                return false;

            // check the error indication
            var rcode = buffer[3] & 0x0f;
            if (rcode != 0)
                return false; // result code is an error

            var qdcount = (ushort)(buffer[4] << 8 | buffer[5]);
            var ancount = (ushort)(buffer[6] << 8 | buffer[7]);
            var nscount = (ushort)(buffer[8] << 8 | buffer[9]);
            var arcount = (ushort)(buffer[10] << 8 | buffer[11]);

            answers = new DnsRecord[ancount];
            var idxAnswer = 0;

            if (ancount == 0)
                return false; // no answers

            var ptr = 12;
            for (var i = 0; i < qdcount; ++i) {
                this.SkipQuestion(buffer, ref ptr);
            }

            for (var i = 0; i < ancount; ++i) {
                answers[idxAnswer++] = this.ProcessAnswerRecord(buffer, ref ptr);
            }

            return true;
        }

        private void SkipQuestion(byte[] buffer, ref int ptr) {
            int count = buffer[ptr++];
            while (count != 0) {
                ptr += count;
                count = buffer[ptr++];
            }
            ptr += 4; // skip past qtype and qclass
        }

        private class DnsRecord {
            public DnsRecord(string name, ushort cls, uint ttl) {
                this.Name = name;
                this.Cls = cls;
                this.TTL = ttl;
            }

            public string Name { get; private set; }
            public ushort Cls { get; private set; }
            public uint TTL { get; private set; }
        }

        private class DnsARecord : DnsRecord {
            public DnsARecord(string name, ushort cls, uint ttl, IPAddress addr)
                : base(name, cls, ttl) => this.Address = addr;

            public IPAddress Address { get; private set; }
        }

        private class DnsPtrRecord : DnsRecord {
            public DnsPtrRecord(string name, ushort cls, uint ttl, string target)
                : base(name, cls, ttl) => this.Target = target;
            public string Target { get; private set; }
        }

        private class DnsNsRecord : DnsRecord {
            public DnsNsRecord(string name, ushort cls, uint ttl, string target)
                : base(name, cls, ttl) => this.Target = target;
            public string Target { get; private set; }
        }

        private class DnsCnameRecord : DnsRecord {
            public DnsCnameRecord(string name, ushort cls, uint ttl, string alias)
                : base(name, cls, ttl) => this.Alias = alias;
            public string Alias { get; private set; }
        }

        private DnsRecord ProcessAnswerRecord(byte[] buffer, ref int ptr) {
            DnsRecord result = null;

            var name = this.GetName(buffer, ref ptr);
            var type = (ushort)(buffer[ptr] << 8 | buffer[ptr + 1]);
            ptr += 2;
            var cls = (ushort)(buffer[ptr] << 8 | buffer[ptr + 1]);
            ptr += 2;
            var ttl = (uint)(buffer[ptr] << 24 | buffer[ptr + 1] << 16 | buffer[ptr + 2] << 8 | buffer[ptr + 3]);
            ptr += 4;

            var rdlen = (ushort)(buffer[ptr] << 8 | buffer[ptr + 1]);
            ptr += 2;

            var idx = ptr;

            switch (type) {
                case (ushort)QTYPE.A: {
                        var addr = (uint)(buffer[idx + 3] << 24 | buffer[idx + 2] << 16 | buffer[idx + 1] << 8 | buffer[idx]);
                        idx += 4;
                        result = new DnsARecord(name, cls, ttl, new IPAddress(addr));
                    }
                    break;
                case (ushort)QTYPE.PTR:
                    result = new DnsPtrRecord(name, cls, ttl, this.GetName(buffer, ref idx));
                    break;
                case (ushort)QTYPE.NS:
                    result = new DnsNsRecord(name, cls, ttl, this.GetName(buffer, ref idx));
                    break;
                case (ushort)QTYPE.SOA: {
                        var soaName = this.GetName(buffer, ref idx);
                        var adminmbx = this.GetName(buffer, ref idx);
                        idx += 20; // skip serno, refresh, retry, expiration, minttl
                    }
                    break;
                case (ushort)QTYPE.MX: {
                        idx += 2; // skip preference
                        var mx = this.GetName(buffer, ref idx);
                    }
                    break;
                case (ushort)QTYPE.CNAME:
                    result = new DnsCnameRecord(name, cls, ttl, this.GetName(buffer, ref idx));
                    break;
                case (ushort)QTYPE.WKS:
                    break;
                case (ushort)QTYPE.SRV:
                    break;
                case (ushort)QTYPE.A6:
                    break;
                default:
                    break;
            }

            ptr += rdlen;

            return result;
        }

        private string GetName(byte[] buffer, ref int ptr) {
            var name = new byte[256];

            var len = 0;
            this.GetLabelSequence(buffer, ref ptr, name, ref len);
            return new string(Encoding.UTF8.GetChars(name));
        }

        private void GetLabelSequence(byte[] buffer, ref int idxBuffer, byte[] result, ref int idxResult) {
            var count = buffer[idxBuffer++];
            while (count != 0) {
                if ((count & 0xC0) == 0xC0) {
                    var idx = ((count & 0x3F) << 8) | buffer[idxBuffer++];
                    this.GetLabelSequence(buffer, ref idx, result, ref idxResult);
                    break;  // A pointer always ends the label sequence
                }
                else {
                    count = (byte)(count & 0x3F);
                    while (count > 0) {
                        result[idxResult++] = buffer[idxBuffer++];
                        --count;
                    }
                    count = buffer[idxBuffer++];
                    if (count != 0)
                        result[idxResult++] = (byte)0x2e;  // '.'
                }
            }
        }

        static private bool Inet_addr(string stringaddress, byte[] ipAddress) {
            if (ipAddress == null)
                throw new Exception();
            int i = 0, index = 0;
            var num = 0;
            var temp = new byte[4];
            while (stringaddress[i] != '.' && stringaddress[i] != 0) {
                while (stringaddress[i] != '.' && stringaddress[i] != 0) {
                    if (stringaddress[i] < '0' || stringaddress[i] > '9') {
                        return false;
                    }
                    num *= 10;
                    num += (int)stringaddress[i] - (int)'0';
                    i++;
                    if (i >= stringaddress.Length)
                        break;
                }
                i++;
                temp[index++] = (byte)num;
                num = 0;
                if (i >= stringaddress.Length)
                    break;

            }
            Array.Copy(temp, ipAddress, 4);
            return true;
        }
    }
}
