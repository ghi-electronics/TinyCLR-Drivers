////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;

    /// <summary>
    /// Represents a connection point (endpoint) on a network as an Internet Protocol (IP) address and a port number.
    /// </summary>
    public class IPEndPoint : EndPoint {
        private const int MinPort = 0x00000000;
        private const int MaxPort = 0x0000FFFF;

        private IPAddress address;
        private int port;
        /// <summary>
        /// Initializes a new instance of the IPEndPoint class from a specified port number and address, with the address specified as an integer.
        /// </summary>
        /// <param name="address">The Internet Protocol (IP) address of a specific Internet host.</param>
        /// <param name="port">The port number associated with a specific IP address, or 0 (zero) to specify any available port. Note that the port parameter value is in host order.</param>
        public IPEndPoint(long address, int port) {
            this.port = port;
            this.address = new IPAddress(address);
        }

        /// <summary>
        /// Initializes a new instance of the IPEndPoint class from a specified port number and address, with the address specified as an IPAddress object.
        /// </summary>
        /// <param name="address">An IPAddress object that specifies the Internet Protocol (IP) address of a specific Internet host.</param>
        /// <param name="port">The port number associated with a specific IP address, or 0 (zero) to specify any available port. Note that the port parameter value is in host order.</param>
        public IPEndPoint(IPAddress address, int port) {
            this.port = port;
            this.address = address;
        }

        /// <summary>
        /// Gets the Internet Protocol (IP) address of the current endpoint.
        /// </summary>
        public IPAddress Address => this.address;
        /// <summary>
        /// Gets the port number of the current endpoint.
        /// </summary>
        public int Port => this.port;

        //public override SocketAddress Serialize()
        //{
        //    // create a new SocketAddress
        //    //
        //    SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetwork, SocketAddress.IPv4AddressSize);
        //    byte[] buffer = socketAddress.m_Buffer;
        //    //
        //    // populate it
        //    //
        //    buffer[2] = unchecked((byte)(this.this.port >> 8));
        //    buffer[3] = unchecked((byte)(this.this.port));

        //    buffer[4] = unchecked((byte)(this.this.address.this.address));
        //    buffer[5] = unchecked((byte)(this.this.address.this.address >> 8));
        //    buffer[6] = unchecked((byte)(this.this.address.this.address >> 16));
        //    buffer[7] = unchecked((byte)(this.this.address.this.address >> 24));

        //    return socketAddress;
        //}
        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => base.GetHashCode();
        /// <summary>
        /// Creates a new instance of the IPEndPoint class from serialized information. 
        /// </summary>
        /// <param name="socketAddress">A SocketAddress object that stores the new endpoint's information in a serial format.</param>
        /// <returns>A new IPEndPoint object that is initialized from the specified SocketAddress object.</returns>
        public override EndPoint Create(SocketAddress socketAddress) {
            // strip out of SocketAddress information on the EndPoint
            //

            var buf = socketAddress.buffer;

            //Debug.Assert(socketAddress.Family == AddressFamily.InterNetwork);

            var port = (int)(
                    (buf[2] << 8 & 0xFF00) |
                    (buf[3])
                    );

            var address = (long)(
                    (buf[4] & 0x000000FF) |
                    (buf[5] << 8 & 0x0000FF00) |
                    (buf[6] << 16 & 0x00FF0000) |
                    (buf[7] << 24)
                    ) & 0x00000000FFFFFFFF;

            var created = new IPEndPoint(address, port);

            return created;
        }
        /// <summary>
        /// Converts an IPEndpoint to a string. <br/>
        /// <strong>IMPORTANT: Use this class only with WIZnet W5100 Ethernet TCP/IP Chip.</strong>
        /// </summary>
        /// <returns>The string containing the IPEndpoint information.</returns>
        public override string ToString() => this.address.ToString() + ":" + this.port.ToString();
        /// <summary>
        /// Tests an IPEndPoint to see if it is equal to the current IPEndPoint. 
        /// </summary>
        /// <param name="obj">Holds the end point to compare.</param>
        /// <returns>true if the endpoints are equal; otherwise false. </returns>
        public override bool Equals(object obj) {
            if (!(obj is IPEndPoint ep)) {
                return false;
            }

            return ep.address.Equals(this.address) && ep.port == this.port;
        }

    }
}
