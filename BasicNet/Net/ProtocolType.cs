////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {

    /// <summary>
    /// Specifies the communications protocol that a Socket object uses to transfer data.<br/>
    /// </summary>
    public enum ProtocolType {

        /// <summary>
        /// The Internet Protocol.
        /// </summary>
        IP = 0,    // dummy for IP

        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6HopByHopOptions = 0,

        /// <summary>
        /// Not Supported.
        /// </summary>
        Icmp = 1,    // control message protocol
        /// <summary>
        /// Not Supported.
        /// </summary>
        Igmp = 2,    // group management protocol
        /// <summary>
        /// Not Supported.
        /// </summary>
        Ggp = 3,    // gateway^2 (deprecated)
        /// <summary>
        /// The Internet Protocol.
        /// </summary>
        IPv4 = 4,

        /// <summary>
        /// The Transmission Control Protocol.
        /// </summary>
        Tcp = 6,    // tcp
        /// <summary>
        /// Not Supported.
        /// </summary>
        Pup = 12,   // pup

        /// <summary>
        /// The User Datagram Protocol
        /// </summary>
        Udp = 17,   // user datagram protocol
        /// <summary>
        /// Not Supported.
        /// </summary>
        Idp = 22,   // xns idp
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6 = 41,   // IPv4
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6RoutingHeader = 43,   // IPv6RoutingHeader
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6FragmentHeader = 44,   // IPv6FragmentHeader
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPSecEncapsulatingSecurityPayload = 50,   // IPSecEncapsulatingSecurityPayload
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPSecAuthenticationHeader = 51,   // IPSecAuthenticationHeader
        /// <summary>
        /// Not Supported.
        /// </summary>
        IcmpV6 = 58,   // IcmpV6
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6NoNextHeader = 59,   // IPv6NoNextHeader
        /// <summary>
        /// Not Supported.
        /// </summary>
        IPv6DestinationOptions = 60,   // IPv6DestinationOptions
        /// <summary>
        /// Not Supported.
        /// </summary>
        ND = 77,   // UNOFFICIAL net disk proto

        /// <summary>
        /// The raw IP-packet protocol.
        /// </summary>
        Raw = 255,  // raw IP packet
        /// <summary>
        /// Not Supported.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// Not Supported.
        /// </summary>
        Ipx = 1000,
        /// <summary>
        /// Not Supported.
        /// </summary>
        Spx = 1256,
        /// <summary>
        /// Not Supported.
        /// </summary>
        SpxII = 1257,
        /// <summary>
        /// Not Supported.
        /// </summary>
        Unknown = -1,   // unknown protocol type
    } // enum ProtocolType
} // namespace System.Net.Sockets


