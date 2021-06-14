////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {
    /// <summary>
    /// Constitutes a container class for information about an Internet host. <br/>
    /// <strong>IMPORTANT: Use this class only with WIZnet W5100 Ethernet TCP/IP Chip.</strong>
    /// </summary>
    /// <remarks>
    /// The IPHostEntry class associates a Domain Name System (DNS) host name with an array of IP addresses.<br/>
    /// Use the IPHostEntry class as a helper class with the Dns class.
    /// </remarks>
    public class IPHostEntry {
        internal string hostName;
        internal IPAddress[] addressList;
        /// <summary>
        /// Gets the Domain Name System (DNS) name of a specific host server.
        /// </summary>
        /// <remarks>
        /// A string that contains the primary host name for the server.
        /// </remarks>
        public string HostName => this.hostName;
        /// <summary>
        /// Gets a list of Internet Protocol (IP) addresses that are associated with a specific host server.
        /// </summary>
        /// <remarks>
        /// An array of IPAddress objects, each of which contains an IP address that resolves to the host name, which is specified by the HostName property.
        /// </remarks>
        public IPAddress[] AddressList => this.addressList;
    }
}


