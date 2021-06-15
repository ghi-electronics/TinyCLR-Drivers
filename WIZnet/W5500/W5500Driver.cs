using System;
using System.Runtime.CompilerServices;
using System.Threading;

using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace GHIElectronics.TinyCLR.Drivers.WIZnet.W5500 {
    public delegate void GlobalEventHandler(object sender);
    public delegate void SocketEventHandler(object sender, int socket);

    internal class W5500Driver {
        private const int W5500_IO_BASE = 0x00;
        private const int SOCKETS_BASE_ADDRESS = 0x400;
        private const int SOCKET_REGISTER_MEMORY_SIZE = 0x100;

        private const byte SPI_READ = 0x00;   // 0 << 2
        private const byte SPI_WRITE = 0x04;  // 1 << 2

        public enum MR : byte {
            RST = 0x80, // reset
            WOL = 0x20, // Wake on lan
            PB = 0x10, // Ping block
            PPPOE = 0x08, // Enable PPPoE
            FARP = 0x02, // Force ARP
        }

        public enum IR : byte {
            CONFLICT = 0x80,
            UNREACHABLE = 0x40,
            PPPoE = 0x20,
            MP = 0x10
        }

        public enum PHYCFGR {
            PHYCFGR_RST = ~(1 << 7),  //< For PHY reset, must operate AND mask.
            PHYCFGR_OPMD = (1 << 6),   // Configre PHY with OPMDC value
            PHYCFGR_OPMDC_ALLA = (7 << 3),
            PHYCFGR_OPMDC_PDOWN = (6 << 3),
            PHYCFGR_OPMDC_NA = (5 << 3),
            PHYCFGR_OPMDC_100FA = (4 << 3),
            PHYCFGR_OPMDC_100F = (3 << 3),
            PHYCFGR_OPMDC_100H = (2 << 3),
            PHYCFGR_OPMDC_10F = (1 << 3),
            PHYCFGR_OPMDC_10H = (0 << 3),
            PHYCFGR_DPX_FULL = (1 << 2),
            PHYCFGR_DPX_HALF = (0 << 2),
            PHYCFGR_SPD_100 = (1 << 1),
            PHYCFGR_SPD_10 = (0 << 1),
            PHYCFGR_LNK_ON = (1 << 0),
            PHYCFGR_LNK_OFF = (0 << 0)
        }

        public enum Sn_MR {
            Sn_MR_CLOSE = 0x00,
            Sn_MR_TCP = 0x01,
            Sn_MR_UDP = 0x02,
            S0_MR_MACRAW = 0x04
        }

        public enum Sn_SR {
            /* Sn_SR values */
            SOCK_CLOSED = 0x00,		/** closed */
            SOCK_INIT = 0x13,		/** init state */
            SOCK_LISTEN = 0x14,		/** listen state */
            SOCK_SYNSENT = 0x15,		/** connection state */
            SOCK_SYNRECV = 0x16,		/** connection state */
            SOCK_ESTABLISHED = 0x17,		/** success to connect */
            SOCK_FIN_WAIT = 0x18,		/** closing state */
            SOCK_CLOSING = 0x1A,		/** closing state */
            SOCK_TIME_WAIT = 0x1B,		/** closing state */
            SOCK_CLOSE_WAIT = 0x1C,		/** closing state */
            SOCK_LAST_ACK = 0x1D,		/** closing state */
            SOCK_UDP = 0x22,		/** udp socket */
            SOCK_IPRAW = 0x32,		/** ip raw mode socket */
            SOCK_MACRAW = 0x42,	/** mac raw mode socket */
            SOCK_PPPOE = 0x5F,		/* pppoe socket */
        }

        public enum Sn_CR {
            OPEN = 0x01,		/** initialize or open socket */
            LISTEN = 0x02,		/** wait connection request in tcp mode(Server mode) */
            CONNECT = 0x04,		/** send connection request in tcp mode(Client mode) */
            DISCON = 0x08,		/** send closing reqeuset in tcp mode */
            CLOSE = 0x10,		/** close socket */
            SEND = 0x20,		/** updata txbuf pointer, send data */
            SEND_MAC = 0x21,		/** send data with MAC address, so without ARP process */
            SEND_KEEP = 0x22,		/**  send keep alive message */
            RECV = 0x40,		/* update rxbuf pointer, recv data */
        }

        public enum Sn_IR {
            SEND_OK = 0x10,		/** complete sending */
            TIMEOUT = 0x08,		/** assert timeout */
            RECV = 0x04,		/** receiving data */
            DISCON = 0x02,		/** closed socket */
            CON = 0x01,		/* established connection */
        }

        public enum Registers : uint {
            // Common register group (global settings)
            MR = (0x0000 << 8), // Mode register
            GAR = (0x0001 << 8), // Gateway address register
            SUBR = (0x0005 << 8), // Subnet mask register
            SHAR = (0x0009 << 8), // Source hardware address
            SIPR = (0x000F << 8), // Source IP address
            INTLEVEL = (0x0013 << 8), // Interrupt assert time
            IR = (0x0015 << 8), // Interrupt register
            IMR = (0x0016 << 8), // Interrupt mask register
            SIR = (0x0017 << 8), // Socket interrupt register
            SIMR = (0x0018 << 8), // Socket interrupt mask register
            RTR = (0x0019 << 8), // Retransmission timeout register in 100's of uS
            PMAGIC = (0x001D << 8), // LCP magic number
            PHAR = (0x001E << 8), // PPP hardware address for PPPoE
            PSID = (0x0024 << 8), // PPP server session id
            UIPR = (0x0028 << 8), // Unreachable IP address
            UPORTR = (0x002C << 8), // Unreachable port
            RCR = (0x001B << 8), // Retry count register
            PTIMER = (0x001C << 8), // LCP echo request timer in 25mS increments
            PHYCFGR = (0x002E << 8), // PHY config register
            VERSIONR = (0x0039 << 8), // Chip version register
        }

        public uint SocketRegisterBlock(SocketRegisters sr, int socketNumber) => (uint)((uint)sr + ((1 + 4 * socketNumber) << 3));
        public byte SocketTxRegisterBlock(int socketNumber) => (byte)((2 + 4 * socketNumber) << 3);
        public byte SocketRxRegisterBlock(int socketNumber) => (byte)((3 + 4 * socketNumber) << 3);

        public enum SocketRegisters : uint {
            MR = (0x0000 << 8), // Mac raw - use only with socket 0
            CR = (0x0001 << 8), // Command register
            IR = (0x0002 << 8), // Socket interrupt register
            SR = (0x0003 << 8), // Status register
            PORT = (0x0004 << 8), // Source port register
            DHAR = (0x0006 << 8), // Destination hardware (MAC) address
            DIPR = (0x000C << 8), // Peer IP address
            DPORT = (0x0010 << 8), // Destination port
            MSSR = (0x0012 << 8), // Max segment size
            TOS = (0x0015 << 8), // Type of service
            TTL = (0x0016 << 8), // Time to live
            RXBUFSZ = (0x001E << 8), // Receive buffer size
            TXBUFSZ = (0x001F << 8), // Transmit buffer size
            TX_FSR = (0x0020 << 8), // Transmit buffer free size
            TX_RD = (0x0022 << 8), // Transmit buffer free size
            TX_WR = (0x0024 << 8), // Transmit buffer write pointer
            RX_RSR = (0x0026 << 8), // Received data size
            RX_RD = (0x0028 << 8), // Receive read pointer
            RX_WR = (0x002A << 8), // Receive write pointer
            IMR = (0x002C << 8), // Interrupt mask register
            FRAG = (0x002D << 8), // IP Fragment field
            KPALVTR = (0x002F << 8), // Keep alive timer
        }

        private SpiDevice spi;
        private GpioPin resetPin;
        private GpioPin interruptPin;
        private int nSockets;
        private int memsize;

        // Use a pre-allocated transfer buffer to minimize allocation churn
        private byte[] buffer = new byte[2048 + 5];  // 2048 max data size, plus 5 for misc command bytes


        /// <summary>
        /// Instantiate a low-level driver for the Wiznet W5500.
        /// </summary>
        /// <param name="sockets">The number of simultaneous sockets requested. Must be 1, 2, 4, or 8. Fewer sockets mean you get larger buffers. The
        /// buffer size will be 16K / sockets</param>
        /// <param name="spiModule">The hardware SPI module to which the W5500 is connected</param>
        /// <param name="csPin">The chip-select pin for the W5500</param>
        /// <param name="resetPin">The reset pin for the W5500. You can omit the reset pin, but this is not recommended and you may need to
        /// power-cycle the W5500 to restore operation in case of errors.</param>
        /// <param name="interruptPin">The interrupt pin for the W5500. The interrupt pin is options, but performance is reduced and certain 
        /// async functionality is not available if the interrupt pin is not configured.</param>
        public W5500Driver(int sockets, SpiController controller, GpioPin csPin, GpioPin resetPin, GpioPin interruptPin) {
            if (sockets != 1 && sockets != 2 && sockets != 4 && sockets != 8)
                throw new ArgumentException("Socket count must be 1, 2, 4, or 8");

            this.nSockets = sockets;

            if (resetPin != null) {
                this.resetPin = resetPin;
                this.resetPin.SetDriveMode(GpioPinDriveMode.Output);
            }

            if (interruptPin != null) {
                this.interruptPin = interruptPin;
                this.interruptPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                this.interruptPin.ValueChangedEdge = GpioPinEdge.FallingEdge;
            }

            // W5500 works with mode0 (sclk idle low, sample on ris) or mode3 (sclk idle high, sample on rise)            

            var settings = new SpiConnectionSettings() {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = csPin,
                Mode = SpiMode.Mode0,
                ClockFrequency = 1_000_000,
                ChipSelectActiveState = false,
            };

            this.spi = controller.GetDevice(settings);

            this.Reset();
        }

        public int SocketBufferSize => this.memsize;

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal byte RegisterRead(Registers reg) {
            this.buffer[0] = (byte)(((uint)reg >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)reg >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)reg & 0xf8) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, this.buffer, 3, 1, 3);
            return this.buffer[3];
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void RegisterRead(Registers reg, byte[] data, int offset, int len) {
            if (data.Length < offset + len)
                throw new ArgumentException("Array to small for the requested length");

            this.buffer[0] = (byte)(((uint)reg >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)reg >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)reg & 0xf8) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, data, offset, len, 3);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal byte SocketRegisterRead(SocketRegisters reg, int socket) {
            var addr = this.SocketRegisterBlock(reg, socket);
            this.buffer[0] = (byte)(((uint)addr >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)addr >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)addr & 0xf8) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, this.buffer, 3, 1, 3);
            return this.buffer[3];
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal ushort SocketRegisterReadUshort(SocketRegisters reg, int socket) {
            var addr = this.SocketRegisterBlock(reg, socket);
            this.buffer[0] = (byte)(((uint)addr >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)addr >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)addr & 0xf8) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, this.buffer, 3, 2, 3);
            return (ushort)(this.buffer[3] << 8 | this.buffer[4]);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void SocketRegisterRead(SocketRegisters reg, int socket, byte[] data, int offset, int len) {
            if (data.Length < offset + len)
                throw new ArgumentException("Array to small for the requested length");

            var addr = this.SocketRegisterBlock(reg, socket);
            this.buffer[0] = (byte)(((uint)addr >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)addr >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)addr & 0xf8) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, data, offset, len, 3);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void RegisterWrite(Registers reg, byte data) {
            this.buffer[0] = (byte)(((uint)reg >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)reg >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)reg & 0xf8) | SPI_WRITE);
            this.buffer[3] = data;
            this.WriteRead(this.buffer, 0, 4, this.buffer, 4, 0, 4);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void RegisterWrite(Registers reg, params byte[] data) {
            this.buffer[0] = (byte)(((uint)reg >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)reg >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)reg & 0xf8) | SPI_WRITE);
            Array.Copy(data, 0, this.buffer, 3, data.Length);
            this.WriteRead(this.buffer, 0, data.Length + 3, this.buffer, data.Length + 3, 0, data.Length + 3);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void SocketRegisterWrite(SocketRegisters reg, int socket, byte data) {
            var addr = this.SocketRegisterBlock(reg, socket);
            this.buffer[0] = (byte)(((uint)addr >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)addr >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)addr & 0xf8) | SPI_WRITE);
            this.buffer[3] = data;
            this.WriteRead(this.buffer, 0, 4, this.buffer, 4, 0, 0);
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void SocketRegisterWrite(SocketRegisters reg, int socket, params byte[] data) {
            var addr = this.SocketRegisterBlock(reg, socket);
            this.buffer[0] = (byte)(((uint)addr >> 16) & 0xff);
            this.buffer[1] = (byte)(((uint)addr >> 8) & 0xff);
            this.buffer[2] = (byte)(((uint)addr & 0xf8) | SPI_WRITE);
            Array.Copy(data, 0, this.buffer, 3, data.Length);
            this.WriteRead(this.buffer, 0, data.Length + 3, this.buffer, data.Length + 3, 0, 0);
        }

        internal void SocketRegisterWriteUshort(SocketRegisters reg, int socket, ushort data) => this.SocketRegisterWrite(reg, socket, (byte)((data >> 8) & 0xff), (byte)(data & 0xff));

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal int SocketSendData(int socket, byte[] data, int offset, int len) {
            ushort ptr;
            var ret = 0;
            ptr = this.SocketRegisterReadUshort(SocketRegisters.TX_WR, socket);
            ret = this.WriteData(socket, ref ptr, data, offset, len);
            this.SocketRegisterWriteUshort(SocketRegisters.TX_WR, socket, ptr);
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal int WriteData(int slot, ref ushort ptr, byte[] src, int offset, int len) {
            if (len > this.memsize)
                throw new Exception("The size of the payload exceeds the socket memory size of " + this.memsize + " bytes");
            var freeSize = this.SocketRegisterReadUshort(SocketRegisters.TX_FSR, slot);
            if (len > freeSize)
                throw new Exception("Insufficient buffer space available");

            this.buffer[0] = (byte)((ptr >> 8) & 0xff);
            this.buffer[1] = (byte)(ptr & 0xff);
            this.buffer[2] = (byte)(this.SocketTxRegisterBlock(slot) | SPI_WRITE);
            Array.Copy(src, offset, this.buffer, 3, len);
            this.WriteRead(this.buffer, 0, len + 3, this.buffer, 0, 0, 0);
            ptr += (ushort)len;

            return len;
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal void ReadData(int slot, ref ushort ptr, byte[] buffer, int offset, int len) {
            this.buffer[0] = (byte)((ptr >> 8) & 0xff);
            this.buffer[1] = (byte)(ptr & 0xff);
            this.buffer[2] = (byte)(this.SocketRxRegisterBlock(slot) | SPI_READ);
            this.WriteRead(this.buffer, 0, 3, buffer, 0, len, 3);
            ptr += (ushort)len;
        }

        public byte GetMR() => this.RegisterRead(Registers.MR);
        public byte GetVERSIONR() => this.RegisterRead(Registers.VERSIONR);

        public byte GetPHYCFGR() => this.RegisterRead(Registers.PHYCFGR);
        public void SetPHYCFGR(byte value) => this.RegisterWrite(Registers.PHYCFGR, value);

        public void Reset() {
            if (this.resetPin != null) {
                this.resetPin.Write(GpioPinValue.Low);
                Thread.Sleep(2);
                this.resetPin.Write(GpioPinValue.High);
                Thread.Sleep(50);
            }

            this.RegisterWrite(Registers.MR, (byte)MR.RST); //soft reset

            var memsize = (byte)(16 / this.nSockets);
            this.memsize = 1024 * memsize;

            // configure buffers
            for (var i = 0; i < 8; ++i) {
                // Set send buffer size
                this.SocketRegisterWrite(SocketRegisters.TXBUFSZ, i, memsize);
                // Set receive buffer size
                this.SocketRegisterWrite(SocketRegisters.RXBUFSZ, i, memsize);
            }

            if (this.interruptPin != null) {
                this.interruptPin.ValueChanged += this.InterruptPin_ValueChanged;
                // Enable all global interrupts
                this.RegisterWrite(Registers.IMR, (byte)(IR.CONFLICT | IR.MP | IR.PPPoE | IR.UNREACHABLE));
                // Enable interrupts on all sockets
                this.RegisterWrite(W5500Driver.Registers.SIMR, (byte)0x0ff);
                // Enable all interrupt types on all sockets
                for (var i = 0; i < 8; ++i) {
                    // Enable all interrupt types
                    this.SocketRegisterWrite(SocketRegisters.IMR, i, (byte)(Sn_IR.SEND_OK | Sn_IR.TIMEOUT | Sn_IR.RECV | Sn_IR.DISCON | Sn_IR.CON));
                }
            }
            // The chip needs time to settle - remove this and things go badly if you send commands to the chip too soon
            Thread.Sleep(2000);
        }

        private void InterruptPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e) {
            var ir = this.RegisterRead(W5500Driver.Registers.IR);

            if ((ir & (byte)IR.CONFLICT) != 0) {
                this.OnAddressConflictDetected?.Invoke(this);
                this.RegisterWrite(W5500Driver.Registers.IR, (byte)IR.CONFLICT);
            }
            if ((ir & (byte)IR.MP) != 0) {
                this.OnWakeOnLanReceived?.Invoke(this);
                this.RegisterWrite(W5500Driver.Registers.IR, (byte)IR.MP);
            }
            if ((ir & (byte)IR.PPPoE) != 0) {
                this.OnPPPoEClosed?.Invoke(this);
                this.RegisterWrite(W5500Driver.Registers.IR, (byte)IR.PPPoE);
            }
            if ((ir & (byte)IR.UNREACHABLE) != 0) {
                this.OnAddressUnreachable?.Invoke(this);
                this.RegisterWrite(W5500Driver.Registers.IR, (byte)IR.UNREACHABLE);
            }

            // Handle per-socket interrupts
            ir = this.RegisterRead(W5500Driver.Registers.SIR);
            for (var i = 0; i < 8; ++i) {
                if ((ir & (1 << i)) != 0) {
                    this.HandleSocketInterrupt(i);
                }
            }         
        }

        private void HandleSocketInterrupt(int socket) {
            var sir = this.SocketRegisterRead(W5500Driver.SocketRegisters.IR, socket);

            // Many of these were removed because we use the IR status for blocking.
            // Having them fire and get cleared as part of interrupt processing is breaking
            //   the wait loops due to a race condition between who clears the flag first.

            //if ((sir & (byte)Sn_IR.SEND_OK) != 0)
            //{

            //    SocketRegisterWrite(SocketRegisters.IR, socket, (byte)Sn_IR.SEND_OK);
            //}
            //if ((sir & (byte)Sn_IR.TIMEOUT) != 0)
            //{
            //    if (OnSocketTimeout != null)
            //        OnSocketTimeout(this, socket);
            //    SocketRegisterWrite(SocketRegisters.IR, socket, (byte)Sn_IR.TIMEOUT);
            //}
            //if ((sir & (byte)Sn_IR.RECV) != 0)
            //{
            //    if (OnSocketDataAvailable != null)
            //        OnSocketDataAvailable(this, socket);
            //    SocketRegisterWrite(SocketRegisters.IR, socket, (byte)Sn_IR.RECV);
            //}
            if ((sir & (byte)Sn_IR.DISCON) != 0) {
                OnSocketDisconnect?.Invoke(this, socket);
                this.SocketRegisterWrite(SocketRegisters.IR, socket, (byte)Sn_IR.DISCON);
            }
            //if ((sir & (byte)Sn_IR.CON) != 0)
            //{
            //    if (OnSocketConnect != null)
            //        OnSocketConnect(this, socket);
            //    SocketRegisterWrite(SocketRegisters.IR, socket, (byte)Sn_IR.CON);
            //}
        }

        public event GlobalEventHandler OnAddressConflictDetected;
        public event GlobalEventHandler OnWakeOnLanReceived;
        public event GlobalEventHandler OnPPPoEClosed;
        public event GlobalEventHandler OnAddressUnreachable;

        public event SocketEventHandler OnSocketDisconnect;

        private void WriteRead(byte[] writeBuffer, int writeOffset, int writeCount, byte[] readBuffer, int readOffset, int readCount, int startReadOffset) {

            var sizeMax = (writeOffset + writeCount) > (startReadOffset + readCount) ? (writeOffset + writeCount) : (startReadOffset + readCount);

            var bw = new byte[sizeMax];

            Array.Copy(writeBuffer, writeOffset, bw, 0, writeCount);
            
            if (readCount > 0) {
                var br = new byte[sizeMax];

                this.spi.TransferFullDuplex(bw, br);

                Array.Copy(br, startReadOffset, readBuffer, readOffset, readCount);
            }
            else 
                this.spi.Write(bw);         
        }
    }
}
