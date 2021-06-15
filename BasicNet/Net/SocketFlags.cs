////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Modified by GHI Electronics LLC and Pervasive Digital LLC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GHIElectronics.TinyCLR.Drivers.BasicNet.Sockets {
    using System;


    /// <summary>
    /// Ignored.
    /// </summary>
    [Flags]
    public enum SocketFlags {

        /// <summary>
        /// Ignored.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// Ignored.
        /// </summary>
        OutOfBand = 0x0001,

        /// <summary>
        /// Ignored.
        /// </summary>
        Peek = 0x0002,
        /// <summary>
        /// Ignored.
        /// </summary>
        DontRoute = 0x0004,

        /// <summary>
        /// Ignored.
        /// </summary>
        MaxIOVectorLength = 0x0010,

        /// <summary>
        /// Ignored.
        /// </summary>
        Truncated = 0x0100,
        /// <summary>
        /// Ignored.
        /// </summary>
        ControlDataTruncated = 0x0200,
        /// <summary>
        /// Ignored.
        /// </summary>
        Broadcast = 0x0400,
        /// <summary>
        /// Ignored.
        /// </summary>
        Multicast = 0x0800,
        /// <summary>
        /// Ignored.
        /// </summary>
        Partial = 0x8000,

    };
}


