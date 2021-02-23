//------------------------------------------------------------------------------ 
// Copyright (C) 2021 GHI Electronics
//
// This file is a modified version from Microsoft.
//
//------------------------------------------------------------------------------

using System;

namespace GHIElectronics.TinyCLR.Drivers.Azure.SAS {
    static class SharedAccessSignatureConstants {
        public const int MaxKeyNameLength = 256;
        public const int MaxKeyLength = 256;
        public const string SharedAccessSignature = "SharedAccessSignature";
        public const string AudienceFieldName = "sr";
        public const string SignatureFieldName = "sig";
        public const string KeyNameFieldName = "skn";
        public const string ExpiryFieldName = "se";
        public const string SignedResourceFullFieldName = SharedAccessSignature + " " + AudienceFieldName;
        public const string KeyValueSeparator = "=";
        public const string PairSeparator = "&";

        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        public static readonly TimeSpan MaxClockSkew = new TimeSpan(0, 5, 0);
    }
}
