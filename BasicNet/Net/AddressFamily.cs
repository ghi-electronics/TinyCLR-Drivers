////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {

    /// <summary>
    ///  Specifies the address families that an instance of the .Sockets class can use.<br/>
    ///  <strong>IMPORTANT: Use this class only with serial/intelligent networking adapters drivers.</strong>
    /// </summary>
    public enum AddressFamily {
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Unknown = -1,   // Unknown
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Unspecified = 0,    // unspecified
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Unix = 1,    // local to host (pipes, portals)
        /// <summary>
        ///  internetwork:IP, UDP, TCP, etc.
        /// </summary>
        InterNetwork = 2,
        /// <summary>
        ///  Not Supported.
        /// </summary>
        ImpLink = 3,    // arpanet imp addresses
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Pup = 4,    // pup protocols: e.g. BSP
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Chaos = 5,    // mit CHAOS protocols
        /// <summary>
        ///  Not Supported.
        /// </summary>
        NS = 6,    // XEROX NS protocols
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Ipx = NS,   // IPX and SPX
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Iso = 7,    // ISO protocols
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Osi = Iso,  // OSI is ISO
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Ecma = 8,    // european computer manufacturers
        /// <summary>
        ///  Not Supported.
        /// </summary>
        DataKit = 9,    // datakit protocols
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Ccitt = 10,   // CCITT protocols, X.25 etc
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Sna = 11,   // IBM SNA
        /// <summary>
        ///  Not Supported.
        /// </summary>
        DecNet = 12,   // DECnet
        /// <summary>
        ///  Not Supported.
        /// </summary>
        DataLink = 13,   // Direct data link interface
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Lat = 14,   // LAT
        /// <summary>
        ///  Not Supported.
        /// </summary>
        HyperChannel = 15,   // NSC Hyperchannel
        /// <summary>
        ///  Not Supported.
        /// </summary>
        AppleTalk = 16,   // AppleTalk
        /// <summary>
        ///  Not Supported.
        /// </summary>
        NetBios = 17,   // NetBios-style addresses
        /// <summary>
        ///  Not Supported.
        /// </summary>
        VoiceView = 18,   // VoiceView
        /// <summary>
        ///  Not Supported.
        /// </summary>
        FireFox = 19,   // FireFox
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Banyan = 21,   // Banyan
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Atm = 22,   // Native ATM Services
        /// <summary>
        ///  Not Supported.
        /// </summary>
        InterNetworkV6 = 23,   // Internetwork Version 6
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Cluster = 24,   // Microsoft Wolfpack
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Ieee12844 = 25,   // IEEE 1284.4 WG AF
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Irda = 26,   // IrDA
        /// <summary>
        ///  Not Supported.
        /// </summary>
        NetworkDesigners = 28,   // Network Designers OSI & gateway enabled protocols
        /// <summary>
        ///  Not Supported.
        /// </summary>
        Max = 29,   // Max
    }; // enum AddressFamily
}


