using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Drivers.BasicNet;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;
using static GHIElectronics.TinyCLR.Drivers.BasicNet.DhcpClient;

namespace GHIElectronics.TinyCLR.Drivers.WIZnet.W5500 {

    public class W5500Controller : INetworkInterface {
        private static int localPort = 5000;

        private readonly W5500Driver w5500;
        private DhcpClient dhcp;
        private Dns dnsResolver;
        private readonly bool reserveSocket;
        private readonly int totalSockets;
        private readonly int nUserSockets;
        private readonly SocketImpl[] sockets;
        private IPAddress[] dns = new IPAddress[2];
        private IPAddress ipAddress = new IPAddress();
        private IPAddress gwAddress = new IPAddress();
        private IPAddress subnetMask = new IPAddress();
        private byte[] macAddr = new byte[6];
        private IPAddress ipAddress2 = new IPAddress();

        /// <summary>
        /// Instantiate a Wiznet W5500 driver with 8 active sockets and 2K buffers for each socket.
        /// </summary>
        /// <param name="spi">The hardware SPI module to which the W5500 is connected</param>
        /// <param name="csPin">The chip-select pin for the W5500</param>
        /// <param name="resetPin">The reset pin for the W5500. You can omit the reset pin, but this is not recommended and you may need to
        /// power-cycle the W5500 to restore operation in case of errors.</param>
        /// <param name="interruptPin">The interrupt pin for the W5500. The interrupt pin is options, but performance is reduced and certain 
        public W5500Controller(SpiController spi, GpioPin csPin, GpioPin resetPin, GpioPin interruptPin)
           : this(spi, csPin, resetPin, interruptPin, 8, false) {
        }

        /// <summary>
        /// Instantiate a Wiznet W5500 driver with 8 active sockets and 2K buffers for each socket.
        /// </summary>
        /// <param name="spi">The hardware SPI module to which the W5500 is connected</param>
        /// <param name="csPin">The chip-select pin for the W5500</param>
        /// <param name="resetPin">The reset pin for the W5500. You can omit the reset pin, but this is not recommended and you may need to
        /// power-cycle the W5500 to restore operation in case of errors.</param>
        /// <param name="interruptPin">The interrupt pin for the W5500. The interrupt pin is options, but performance is reduced and certain 
        /// async functionality is not available if the interrupt pin is not configured.</param>
        /// <param name="reserveSocket">Set to true to reserve one socket for DHCP and DNS operations</param>
        /// buffer size will be 16K / sockets</param>
        public W5500Controller(SpiController spi, GpioPin csPin, GpioPin resetPin, GpioPin interruptPin, bool reserveSocket)
            : this(spi, csPin, resetPin, interruptPin, 8, reserveSocket) {
        }

        /// <summary>
        /// Instantiate a Wiznet W5500 driver.
        /// </summary>
        /// <param name="spi">The hardware SPI module to which the W5500 is connected</param>
        /// <param name="csPin">The chip-select pin for the W5500</param>
        /// <param name="resetPin">The reset pin for the W5500. You can omit the reset pin, but this is not recommended and you may need to
        /// power-cycle the W5500 to restore operation in case of errors.</param>
        /// <param name="interruptPin">The interrupt pin for the W5500. The interrupt pin is options, but performance is reduced and certain 
        /// async functionality is not available if the interrupt pin is not configured.</param>
        /// <param name="socketsDesired">The number of simultaneous sockets requested. Must be 1, 2, 4, or 8. Fewer sockets mean you get larger buffers. The
        /// <param name="reserveSocket">Set to true to reserve one socket for DHCP and DNS operations</param>
        public W5500Controller(SpiController spi, GpioPin csPin, GpioPin resetPin, GpioPin interruptPin, int socketsDesired = 8, bool reserveSocket = false) {
            if (socketsDesired != 1 && socketsDesired != 2 && socketsDesired != 4 && socketsDesired != 8)
                throw new ArgumentException("Socket count must be 1, 2, 4, or 8");
            if (reserveSocket && socketsDesired == 1)
                throw new ArgumentException("If you reserve a socket, then you must have socketsDesired >= 2");
            this.totalSockets = socketsDesired;
            this.sockets = new SocketImpl[this.totalSockets];
            this.reserveSocket = reserveSocket;
            this.nUserSockets = this.reserveSocket ? this.totalSockets - 1 : this.totalSockets;

            this.w5500 = new W5500Driver(this.totalSockets, spi, csPin, resetPin, interruptPin);

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISocketImpl CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool useReservedSocket) {
            var iSlot = -1;
            // MACRAW mode must use socket 0
            if (protocolType == ProtocolType.Raw) {
                if (this.sockets[0] == null)
                    iSlot = 0;
                else
                    throw new Exception("Socket 0 is required but is not available");
            }
            // if the caller requested the reserved socket, and we have it reserved, and it is free...
            if (iSlot == -1 && useReservedSocket && this.reserveSocket && this.sockets[this.totalSockets - 1] == null) {
                iSlot = this.totalSockets - 1;
            }
            // if the caller wanted the reserved socket and it wasn't available OR the called didn't request a reserved socket
            //   then just take the next available socket.
            if (iSlot == -1) {
                for (var i = 0; i < this.nUserSockets; ++i) {
                    if (this.sockets[i] == null) {
                        iSlot = i;
                        break;
                    }
                }
            }
            // No sockets of any type are available
            if (iSlot == -1)
                throw new Exception("All sockets in use");

            byte mode = 0x00;
            switch (protocolType) {
                case ProtocolType.Udp:
                    if (socketType == SocketType.Dgram)
                        mode |= (byte)W5500Driver.Sn_MR.Sn_MR_UDP;
                    else
                        throw new Exception("Invalid socket type");
                    break;
                case ProtocolType.Tcp:
                    if (socketType == SocketType.Stream)
                        mode |= (byte)W5500Driver.Sn_MR.Sn_MR_TCP;
                    else
                        throw new Exception("Invalid socket type");
                    break;
                case ProtocolType.Raw:
                    if (iSlot != 0)
                        throw new Exception("Must use socket 0 for MACRAW");
                    mode |= (byte)W5500Driver.Sn_MR.S0_MR_MACRAW;
                    break;
                default:
                    throw new Exception("Unsupported protocol type");
            }

            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.MR, iSlot, mode);

            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, iSlot, (byte)W5500Driver.Sn_CR.OPEN);
            // wait for the command to be acknowledged
            while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, iSlot) != 0) ;

            this.sockets[iSlot] = new SocketImpl(this, iSlot, addressFamily, socketType, protocolType);

            return this.sockets[iSlot];
        }

        public void Listen(int slot) {
            if (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot) == (byte)W5500Driver.Sn_SR.SOCK_INIT) {
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.LISTEN);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot) != (byte)W5500Driver.Sn_SR.SOCK_LISTEN) {
                    if (((byte)this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.TIMEOUT) == (byte)W5500Driver.Sn_IR.TIMEOUT) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.TIMEOUT);
                        throw new Exception("connection timeout");
                    }
                    if ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.DISCON) == (byte)W5500Driver.Sn_IR.DISCON) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.DISCON);
                        throw new Exception("socket closed");
                    }
                }
            }
            else {
                new Exception("socket not initialized");
            }
        }

        public ISocketImpl Accept(int slot, ProtocolType protocolType) {
            var result = this.CreateSocket(AddressFamily.InterNetwork, SocketType.Stream, protocolType, false);
            var sockimpl = (SocketImpl)result;
            var newSlot = sockimpl.SocketNumber;

            var port = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.PORT, slot);
            this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, newSlot, port);

            if (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, newSlot) == (byte)W5500Driver.Sn_SR.SOCK_INIT) {
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, newSlot, (byte)W5500Driver.Sn_CR.LISTEN);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, newSlot) != 0) ;
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, newSlot) != (byte)W5500Driver.Sn_SR.SOCK_LISTEN) {
                    if ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, newSlot) & (byte)W5500Driver.Sn_IR.TIMEOUT) == (byte)W5500Driver.Sn_IR.TIMEOUT) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, newSlot, (byte)W5500Driver.Sn_IR.TIMEOUT);
                        throw new Exception("connection timeout");
                    }
                    if ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, newSlot) & (byte)W5500Driver.Sn_IR.DISCON) == (byte)W5500Driver.Sn_IR.DISCON) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, newSlot, (byte)W5500Driver.Sn_IR.DISCON);
                        throw new Exception("socket closed");
                    }
                }
            }
            else {
                throw new Exception("accept failed");
            }
            return result;
        }

        public void Close(int slot) {
            var status = this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot);
            if (status == (byte)W5500Driver.Sn_SR.SOCK_UDP) {
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.CLOSE);
                // wait for the command to be acknowledged
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;
            }
            else if (status != 0x00) {
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.DISCON);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0)
                    Thread.Sleep(5);  // this can take a while, so yield the processor
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.CLOSE);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot) != (byte)W5500Driver.Sn_SR.SOCK_CLOSED) {
                    this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, 0xff);
                }
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, 0xff);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void ReleaseSocket(int slot) => this.sockets[slot] = null;

        public EndPoint GetLocalEndpoint(int socket) {
            this.w5500.RegisterRead(W5500Driver.Registers.SIPR, this.ipAddress.GetAddressBytes(), 0, 4);
            var port = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.PORT, socket);
            return new IPEndPoint(this.ipAddress, port);
        }

        public EndPoint GetRemoteEndpoint(int socket) {
            var ip = new IPAddress();
            this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.DIPR, socket, ip.GetAddressBytes(), 0, 4);
            var port = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.PORT, socket);
            return new IPEndPoint(ip, port);
        }

        public void Bind(int socket, EndPoint localEP) {
            if (!(localEP is IPEndPoint ep))
                throw new ArgumentException("unsupported endpoint type");

            var port = (ushort)(ep.Port & 0xffff);
            this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, socket, port);
        }

        public void Connect(int slot, ref EndPoint localEP, EndPoint remoteEP) {
            var addr = ((IPEndPoint)remoteEP).Address.GetAddressBytes();
            var port = ((IPEndPoint)remoteEP).Port;

            if (
            ((addr[0] == 0xFF) && (addr[1] == 0xFF) && (addr[2] == 0xFF) && (addr[3] == 0xFF)) ||
            ((addr[0] == 0x00) && (addr[1] == 0x00) && (addr[2] == 0x00) && (addr[3] == 0x00)) ||
            (port == 0x00)) {
                throw new Exception("invalid ip address or port");
            }

            if (localEP == null) {
                W5500Controller.localPort++; // if don't set the source port, set local_port number.
                this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, slot, (ushort)(W5500Controller.localPort & 0xffff));
                localEP = new IPEndPoint(this.ipAddress, W5500Controller.localPort);
            }
            else {
                var lport = ((IPEndPoint)localEP).Port;
                this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, slot, (ushort)(lport & 0xffff));
            }
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.DIPR, slot, addr);
            this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.DPORT, slot, (ushort)port);

            if (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.MR, slot) == (byte)W5500Driver.Sn_MR.Sn_MR_TCP) {
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.CONNECT);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot) != (byte)W5500Driver.Sn_SR.SOCK_ESTABLISHED) {
                    if (((byte)this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.TIMEOUT) == (byte)W5500Driver.Sn_IR.TIMEOUT) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.TIMEOUT);
                        throw new Exception("timeout while awaiting connection");
                    }
                    if (((byte)this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.DISCON) == (byte)W5500Driver.Sn_IR.DISCON) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, (byte)W5500Driver.Sn_IR.DISCON);
                        throw new Exception("socket disconnected");
                    }
                }
            }
        }

        public int Send(int slot, byte[] buffer, int offset, int len, SocketFlags socketFlags, int timeout) {
            var result = len;

            if (len > this.w5500.SocketBufferSize)
                throw new Exception("Send exceeds buffer size");

            var freeSize = 0;
            do {
                freeSize = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.TX_FSR, slot);
                var status = this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot);
                if ((status != (byte)W5500Driver.Sn_SR.SOCK_ESTABLISHED) && (status != (byte)W5500Driver.Sn_SR.SOCK_CLOSE_WAIT)) {
                    return 0;
                }
            } while (freeSize < result);

            // copy data
            result = this.w5500.SocketSendData(slot, buffer, offset, len);
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.SEND);
            // wait for the command to be acknowledged
            while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;

            while ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.SEND_OK) != (byte)(byte)W5500Driver.Sn_IR.SEND_OK) {

                /* m2008.01 [bj] : reduce code */
                if (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.SR, slot) == (byte)W5500Driver.Sn_SR.SOCK_CLOSED) {
                    this.Close(slot);
                    throw new Exception("Socket is closed");
                }
                if ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.TIMEOUT) != 0) {
                    this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)(W5500Driver.Sn_IR.SEND_OK | W5500Driver.Sn_IR.TIMEOUT)); // clear SEND_OK & TIMEOUT
                    return -1;
                }
            }
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.SEND_OK);

            return result;
        }

        public int SendTo(int slot, byte[] buffer, int offset, int len, SocketFlags socketFlags, int timeout, ref EndPoint localEP, EndPoint remoteEP) {
            var ret = -1;

            var addr = ((IPEndPoint)remoteEP).Address.GetAddressBytes();
            var port = ((IPEndPoint)remoteEP).Port;

            //if (count > W5100.SSIZE[s]) ret = W5100.SSIZE[s]; // check size not to exceed MAX size.
            //else ret = count;

            if (((addr[0] == 0x00) && (addr[1] == 0x00) && (addr[2] == 0x00) && (addr[3] == 0x00)) ||
                    ((port == 0x00)) || (len == 0)) {
                throw new Exception("Invalid endpoint addr");
            }
            else {
                if (localEP == null) {
                    W5500Controller.localPort++; // if don't set the source port, set local_port number.
                    this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, slot, (ushort)(W5500Controller.localPort & 0xffff));
                    localEP = new IPEndPoint(this.ipAddress, W5500Controller.localPort);
                }
                else {
                    var lport = ((IPEndPoint)localEP).Port;
                    this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.PORT, slot, (ushort)(lport & 0xffff));
                }
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.DIPR, slot, addr);
                this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.DPORT, slot, (ushort)port);

                // copy data
                ret = this.w5500.SocketSendData(slot, buffer, offset, len);
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.SEND);
                // wait for the command to be acknowledged
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;

                // we will block here waiting for the send to complete
                while ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.SEND_OK) == 0) {
                    if ((this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot) & (byte)W5500Driver.Sn_IR.TIMEOUT) != 0) {
                        this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)(W5500Driver.Sn_IR.SEND_OK | W5500Driver.Sn_IR.TIMEOUT)); /* clear SEND_OK & TIMEOUT */
                        //throw new Exception("Send fail");
                        return -1;
                    }
                }
                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.SEND_OK);
            }

            return ret;
        }

        private bool WaitForData(int slot, int timeout) {
            // wait for data to become available
            var start = DateTime.UtcNow.Ticks;
            byte sir = 0;
            do {
                if (timeout > 0) {
                    var elapsed = DateTime.UtcNow.Ticks - start;
                    if (elapsed > timeout)
                        return false;
                }
                sir = this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.IR, slot);
            } while ((sir & (byte)W5500Driver.Sn_IR.RECV) == 0);
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.IR, slot, (byte)W5500Driver.Sn_IR.RECV);
            return true;
        }


        public byte[] Receive(int slot, SocketFlags socketFlags, int timeout) {
            byte[] buffer = null;

            if (!this.WaitForData(slot, timeout))
                return null;

            var cbAvailable = this.GetBytesAvailable(slot);
            if (cbAvailable > 0) {
                buffer = new byte[cbAvailable];
                var ptr = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.RX_RD, slot);
                this.w5500.ReadData(slot, ref ptr, buffer, 0, cbAvailable);
                this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.RX_RD, slot, ptr);

                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, (byte)W5500Driver.Sn_CR.RECV);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;
            }

            return buffer;
        }

        public int Receive(int slot, byte[] buffer, int offset, int count, SocketFlags socketFlags, int timeout) {
            if (!this.WaitForData(slot, timeout))
                return -1;

            var cbAvailable = this.GetBytesAvailable(slot);
            if (cbAvailable < count)
                count = cbAvailable;
            if (cbAvailable > 0) {
                var ptr = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.RX_RD, slot);
                this.w5500.ReadData(slot, ref ptr, buffer, 0, count);
                this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.RX_RD, slot, ptr);

                this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, (byte)W5500Driver.Sn_CR.RECV);
                while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;
            }

            return count;
        }

        public byte[] ReceiveFrom(int slot, SocketFlags socketFlags, int timeout, ref EndPoint remoteEP) {
            var header = new byte[8];
            var addr = new byte[4];
            int port;
            var dataLen = 0;
            byte[] buffer = null;

            if (!this.WaitForData(slot, timeout))
                return null;

            var ptr = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.RX_RD, slot);
            switch (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.MR, slot) & 0x07) {
                case (int)W5500Driver.Sn_MR.Sn_MR_UDP:
                    // read the eight byte header
                    this.w5500.ReadData(slot, ref ptr, header, 0, 8);
                    addr[0] = header[0];
                    addr[1] = header[1];
                    addr[2] = header[2];
                    addr[3] = header[3];
                    port = (header[4] << 8) | header[5];
                    remoteEP = new IPEndPoint(new IPAddress(addr), port);

                    // read the data
                    dataLen = (ushort)((header[6] << 8) | header[7]);
                    buffer = new byte[dataLen];
                    this.w5500.ReadData(slot, ref ptr, buffer, 0, dataLen);

                    this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.RX_RD, slot, ptr);

                    break;
                default:
                    break;
            }
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.RECV);
            while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;

            return buffer;
        }

        public int ReceiveFrom(int slot, byte[] buffer, int offset, int len, SocketFlags socketFlags, int timeout, ref EndPoint remoteEP) {
            var header = new byte[8];
            var addr = new byte[8];
            int port;
            var dataLen = 0;

            if (!this.WaitForData(slot, timeout))
                return -1;

            var ptr = this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.RX_RD, slot);
            switch (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.MR, slot) & 0x07) {
                case (int)W5500Driver.Sn_MR.Sn_MR_UDP:
                    // read the eight byte header
                    this.w5500.ReadData(slot, ref ptr, header, 0, 8);
                    addr[0] = header[0];
                    addr[1] = header[1];
                    addr[2] = header[2];
                    addr[3] = header[3];
                    port = (header[4] << 8) | header[5];
                    remoteEP = new IPEndPoint(new IPAddress(addr), port);

                    // read the data
                    dataLen = (ushort)((header[6] << 8) | header[7]);
                    this.w5500.ReadData(slot, ref ptr, buffer, offset, dataLen);

                    this.w5500.SocketRegisterWriteUshort(W5500Driver.SocketRegisters.RX_RD, slot, ptr);

                    break;
                default:
                    break;
            }
            this.w5500.SocketRegisterWrite(W5500Driver.SocketRegisters.CR, slot, (byte)W5500Driver.Sn_CR.RECV);
            while (this.w5500.SocketRegisterRead(W5500Driver.SocketRegisters.CR, slot) != 0) ;

            return dataLen;
        }

        public ushort GetBytesAvailable(int slot) => this.w5500.SocketRegisterReadUshort(W5500Driver.SocketRegisters.RX_RSR, slot);

        public DhcpClient DhcpClient => this.dhcp;

        public Dns Dns {
            get {
                if (this.dnsResolver == null)
                    this.dnsResolver = new Dns(this);
                return this.dnsResolver;
            }
        }

        public IPAddress Address {
            get {
                this.w5500.RegisterRead(W5500Driver.Registers.SIPR, this.ipAddress.GetAddressBytes(), 0, 4);
                return this.ipAddress;
            }
            set {
                this.ipAddress = value;
                this.w5500.RegisterWrite(W5500Driver.Registers.SIPR, value.GetAddressBytes());
            }
        }

        public IPAddress SubnetMask {
            get {
                this.w5500.RegisterRead(W5500Driver.Registers.SUBR, this.subnetMask.GetAddressBytes(), 0, 4);
                return this.subnetMask;
            }
            set {
                this.subnetMask = value;
                this.w5500.RegisterWrite(W5500Driver.Registers.SUBR, value.GetAddressBytes());
            }
        }

        public IPAddress GatewayAddress {
            get {
                this.w5500.RegisterRead(W5500Driver.Registers.GAR, this.gwAddress.GetAddressBytes(), 0, 4);
                return this.gwAddress;
            }
            set {
                this.gwAddress = value;
                this.w5500.RegisterWrite(W5500Driver.Registers.GAR, value.GetAddressBytes());
            }
        }

        public IPAddress PrimaryDnsServer {
            get => this.dns[0];
            set => this.dns[0] = value;
        }

        public IPAddress SecondaryDnsServer {
            get => this.dns[1];
            set => this.dns[1] = value;
        }

        public byte[] PhysicalAddress {
            get {
                this.w5500.RegisterRead(W5500Driver.Registers.SHAR, this.macAddr, 0, this.macAddr.Length);
                return this.macAddr;
            }
            set {
                if (value.Length != 6)
                    throw new ArgumentException("value must be six bytes in length");

                this.w5500.RegisterWrite(W5500Driver.Registers.SHAR, value);
            }
        }

        public void EnableStaticIP(IPAddress addr, IPAddress subnet, IPAddress gateway, byte[] macAddr) {
            this.Address = addr;
            this.SubnetMask = subnet;
            this.GatewayAddress = gateway;
            this.PhysicalAddress = macAddr;

            this.OnNetworkAddressChanged(this, new NetworkAddressChangedEventArgs(DateTime.Now));
        }

        public void EnableDhcp() => this.EnableDhcp(null);

        public void EnableDhcp(string hostname) {
            if (this.dhcp == null) {
                this.dhcp = new DhcpClient(this, hostname);

                this.dhcp.OnAddressChangedEventHandler += this.DhcpAddressChangedEventHandler;
            }
        }


        public void EnableStaticDns(IPAddress dns1) {
            this.dns[0] = dns1;
            this.dns[1] = null;
        }

        public void EnableStaticDns(IPAddress dns1, IPAddress dns2) {
            this.dns[0] = dns1;
            this.dns[1] = dns2;
        }

        public sealed class NetworkAddressChangedEventArgs : EventArgs {
            public DateTime Timestamp { get; }

            internal NetworkAddressChangedEventArgs(DateTime timestamp) => this.Timestamp = timestamp;
        }

        public delegate void NetworkAddressChangedEventHandler(INetworkInterface sender, NetworkAddressChangedEventArgs e);
        private void OnNetworkAddressChanged(INetworkInterface sender, NetworkAddressChangedEventArgs e) {
            if (this.ipAddress.Equals(this.ipAddress2) == false) {
                this.ipAddress2 = this.ipAddress;
                this.NetworkAddressChanged?.Invoke(sender, e);
            }
        }
        public event NetworkAddressChangedEventHandler NetworkAddressChanged;
        private void DhcpAddressChangedEventHandler() => this.OnNetworkAddressChanged(this, new NetworkAddressChangedEventArgs(DateTime.Now));

    }
}
