////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets;

namespace GHIElectronics.TinyCLR.Drivers.BasicNet {

    /// <summary>
    /// Provides members you can use when working with Internet Protocol (IP) addresses. <br/>
    /// <strong>IMPORTANT: Use this class only with WIZnet W5100 Ethernet TCP/IP Chip.</strong>
    /// </summary>
    public class IPAddress {

        /// <summary>
        /// Any IP address (0.0.0.0)
        /// </summary>
        public static readonly IPAddress Any = new IPAddress(0x0000000000000000);

        //public static readonly IPAddress Loopback = new IPAddress(0x000000000100007F);
        internal long address;

        public IPAddress()
            : this(0) {
        }

        /// <summary>
        /// Initializes a new instance of the IPAddress class, using a specified Internet Protocol (IP) address.
        /// </summary>
        /// <param name="newAddress"></param>
        public IPAddress(long newAddress) {
            if (newAddress < 0 || newAddress > 0x00000000FFFFFFFF) {
                throw new ArgumentOutOfRangeException();
            }

            this.address = newAddress;
        }

        /// <summary>
        /// Initializes a new instance of the IPAddress class, using a specified Internet Protocol (IP) address.
        /// </summary>
        /// <param name="newAddressBytes"></param>
        public IPAddress(byte[] newAddressBytes)
            : this(((((newAddressBytes[3] << 0x18) | (newAddressBytes[2] << 0x10)) | (newAddressBytes[1] << 0x08)) | newAddressBytes[0]) & ((long)0xFFFFFFFF)) {
        }
        /// <summary>
        /// Compares a specified IP address with the current IP address.
        /// </summary>
        /// <param name="obj">An object that specifies the IP address to be compared with the current IP address.</param>
        /// <returns>true if the two IP addresses are equal; otherwise, false.</returns>
        public override bool Equals(object obj) {
            var addr = obj as IPAddress;

            if (obj == null) return false;

            return this.address == addr.address;
        }
        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Provides a copy of the IPAddress as an array of bytes. 
        /// </summary>
        /// <returns>A copy of the IPAddress as an array of bytes. </returns>
        public byte[] GetAddressBytes() => new byte[]
            {
                (byte)(this.address),
                (byte)(this.address >> 8),
                (byte)(this.address >> 16),
                (byte)(this.address >> 24)
            };

        /// <summary>
        /// Parses an IP address. 
        /// </summary>
        /// <param name="ipString">Holds an IP address in a string.</param>
        /// <returns>The parsed IP address.</returns>
        public static IPAddress Parse(string ipString) {
            if (ipString == null)
                throw new ArgumentNullException();

            ulong ipAddress = 0L;
            var lastIndex = 0;
            var shiftIndex = 0;
            ulong mask = 0x00000000000000FF;
            ulong octet = 0L;
            var length = ipString.Length;

            for (var i = 0; i < length; ++i) {
                // Parse to '.' or end of IP address
                if (ipString[i] == '.' || i == length - 1)
                    // If the IP starts with a '.'
                    // or a segment is longer than 3 characters or shiftIndex > last bit position throw.
                    if (i == 0 || i - lastIndex > 3 || shiftIndex > 24) {
                        throw new ArgumentException();
                    }
                    else {
                        i = i == length - 1 ? ++i : i;
                        octet = (ulong)(ConvertStringToInt32(ipString.Substring(lastIndex, i - lastIndex)) & 0x00000000000000FF);
                        ipAddress = ipAddress + (ulong)((octet << shiftIndex) & mask);
                        lastIndex = i + 1;
                        shiftIndex = shiftIndex + 8;
                        mask = (mask << 8);
                    }
            }

            return new IPAddress((long)ipAddress);
        }
        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>IP address in this format "x.x.x.x"</returns>
        public override string ToString() => ((byte)(this.address)).ToString() +
                    "." +
                    ((byte)(this.address >> 8)).ToString() +
                    "." +
                    ((byte)(this.address >> 16)).ToString() +
                    "." +
                    ((byte)(this.address >> 24)).ToString();

        //--//
        ////////////////////////////////////////////////////////////////////////////////////////
        // this method ToInt32 is part of teh Convert class which we will bring over later
        // at that time we will get rid of this code
        //

        private static int ConvertStringToInt32(string value) {
            var num = value.ToCharArray();
            var result = 0;

            var isNegative = false;
            var signIndex = 0;

            if (num[0] == '-') {
                isNegative = true;
                signIndex = 1;
            }
            else if (num[0] == '+') {
                signIndex = 1;
            }

            var exp = 1;
            for (var i = num.Length - 1; i >= signIndex; i--) {
                if (num[i] < '0' || num[i] > '9') {
                    throw new ArgumentException();
                }

                result += ((num[i] - '0') * exp);
                exp *= 10;
            }

            return (isNegative) ? (-1 * result) : result;
        }

        // this method ToInt32 is part of teh Convert class which we will bring over later
        ////////////////////////////////////////////////////////////////////////////////////////
    } // class IPAddress
} // namespace System.Net


