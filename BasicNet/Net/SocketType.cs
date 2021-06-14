////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {

    /// <summary>
    /// Specifies the socket type, which defines the capabilities of a socket that is used in network communications. 
    /// </summary>
    public enum SocketType {

        /// <summary>
        /// A socket that supports reliable, two-way, connection-based byte streams without the duplication of data and without preservation of boundaries. A socket of this type communicates with a single peer and requires a remote host connection before communication can begin. The Stream socket type uses the InterNetwork address family and the Transmission Control Protocol (TCP).
        /// </summary>
        Stream = 1,    // stream socket
        /// <summary>
        /// A socket that supports datagrams, which are connectionless, unreliable messages of a fixed (typically small) maximum length. Messages might be lost or duplicated and might arrive out of order. A socket of the Dgram type requires no connection before sending and receiving data, and it can communicate with multiple peers. The Dgram socket type uses the InterNetwork address family and the User Datagram Protocol (UDP).
        /// </summary>
        Dgram = 2,    // datagram socket
        /// <summary>
        /// A socket that supports access to the underlying transport protocol. The Raw socket type supports communication that uses protocols such as Internet Control Message Protocol (ICMP) and Internet Group Management Protocol (IGMP). Your application must provide a complete Internet Protocol (IP) header when sending data. Received datagrams return with the IP header and options intact.
        /// </summary>
        Raw = 3,    // raw-protocolinterface
        /// <summary>
        /// A socket that supports connectionless, message-oriented, reliably delivered messages (RDMs) and preserves message boundaries in data. When you use the Rdm socket type, messages arrive unduplicated and in order. Furthermore, the sender is notified if messages are lost. If you initialize a socket with the Rdm socket type, you do not need a remote host connection before sending and receiving data. The Rdm socket type also enables you to communicate with multiple peers.
        /// </summary>
        Rdm = 4,    // reliably-delivered message
        /// <summary>
        /// A socket that provides connection-oriented and reliable two-way transfer of ordered byte streams across a network. The Seqpacket socket type does not duplicate data, and it preserves boundaries within the data stream. A socket of the Seqpacket type communicates with a single peer and requires a remote host connection before communication can begin.
        /// </summary>
        Seqpacket = 5,    // sequenced packet stream
        /// <summary>
        /// A socket of an unknown type.
        /// </summary>
        Unknown = -1,   // Unknown socket type

    } // enum SocketType

} 


