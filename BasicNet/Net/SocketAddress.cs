////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;

    /// <summary>
    /// Represents a network connection point (endpoint) in serialized form. More specifically, the SocketAddress class stores the endpoint's information in a serial format. <br/>
    /// <strong>IMPORTANT: Use this class only with WIZnet W5100 Ethernet TCP/IP Chip.</strong>
    /// </summary>
    public class SocketAddress {
        internal const int IPv4AddressSize = 16;

        internal byte[] buffer;
        /// <summary>
        /// Gets the address family for the current address.
        /// </summary>
        public AddressFamily Family => (AddressFamily)((this.buffer[1] << 8) | this.buffer[0]);

        internal SocketAddress(byte[] address) => this.buffer = address;

        /// <summary>
        /// Initializes a new instance of the SocketAddress class. 
        /// </summary>
        /// <param name="family">A value of the AddressFamily enumeration specifying the addressing scheme that is used to resolve the address.</param>
        /// <param name="size">The number of bytes to be allocated for the underlying buffer, which is used to store the address information. Two bytes of the buffer are used to store the value of the family parameter.</param>
        public SocketAddress(AddressFamily family, int size) {
            //Microsoft.SPOT.Debug.Assert(size > 2);

            this.buffer = new byte[size]; //(size / IntPtr.Size + 2) * IntPtr.Size];//sizeof DWORD

            this.buffer[0] = unchecked((byte)((int)family));
            this.buffer[1] = unchecked((byte)((int)family >> 8));
        }
        /// <summary>
        /// Gets the underlying buffer size of the SocketAddress. 
        /// </summary>
        public int Size => this.buffer.Length;
        /// <summary>
        /// Gets or sets the specified index element in the underlying buffer.
        /// </summary>
        /// <param name="offset">Holds the array index element of the desired information.</param>
        /// <returns></returns>
        public byte this[int offset] {
            get => this.buffer[offset];
            set => this.buffer[offset] = value;
        }

    } // class SocketAddress
} // namespace System.Net


