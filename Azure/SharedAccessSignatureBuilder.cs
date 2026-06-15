//------------------------------------------------------------------------------ 
// Copyright (C) 2021 GHI Electronics
//
// This file is a modified version from Microsoft.
//
//------------------------------------------------------------------------------

using System;
using System.Text;
using System.Net;
using System.Security.Cryptography;

namespace GHIElectronics.TinyCLR.Drivers.Azure.SAS
{
    public class SharedAccessSignatureBuilder
    {
        private string key;

        /// <summary>
        /// Initializes a new instance of <see cref="SharedAccessSignatureBuilder"/> class.
        /// </summary>
        public SharedAccessSignatureBuilder() => this.TimeToLive = TimeSpan.FromMinutes(60);

        /// <summary>
        /// The shared access policy name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// The shared access key value.
        /// </summary>
        public string Key {
            get => this.key;

            set =>
                // TQD StringValidationHelper.EnsureBase64String(value, "Key");
                this.key = value;
        }

        /// <summary>
        /// The resource Id being accessed.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The time the token expires.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Build a SAS token.
        /// </summary>
        /// <returns>SAS token.</returns>
        public string ToSignature() => this.BuildSignature(this.KeyName, this.Key, this.Target, this.TimeToLive);

        private string BuildSignature(string keyName, string key, string target, TimeSpan timeToLive) {
            var expiresOn = BuildExpiresOn(timeToLive);
            var audience = WebUtility.UrlEncode(target);

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>
            var request = audience + "\n" + expiresOn;

            var signature = this.Sign(request, key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();

            buffer.Append(string.Format("{0} {1}={2}&{3}={4}&{5}={6}",
             SharedAccessSignatureConstants.SharedAccessSignature,
                SharedAccessSignatureConstants.AudienceFieldName, audience,
              SharedAccessSignatureConstants.SignatureFieldName, WebUtility.UrlEncode(signature),
            SharedAccessSignatureConstants.ExpiryFieldName, WebUtility.UrlEncode(expiresOn)));

            if (!this.IsNullOrWhiteSpace(keyName)) {
                buffer.Append(string.Format("&{0}={1}",
                    SharedAccessSignatureConstants.KeyNameFieldName, WebUtility.UrlEncode(keyName)));
            }

            return buffer.ToString();
        }

        private static string BuildExpiresOn(TimeSpan timeToLive) {
            var expiresOn = DateTime.UtcNow.Add(timeToLive);
            var secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            var seconds = (long)secondsFromBaseTime.TotalSeconds;
            return seconds.ToString();
        }

        // RFC 4648 §4 Base64 alphabet. TinyCLR's Convert.ToBase64String
        // defaults to a NETMF-era nonstandard alphabet ('!' / '*' instead
        // of '+' / '/') and exposes a UseRFC4648Encoding toggle to switch
        // to this one. Desktop BCL doesn't have that toggle — its
        // Convert.ToBase64String is already RFC 4648. To keep this driver
        // working under both runtimes (the dual-mode Desktop sibling must
        // not bind to TinyCLR-only members or the JIT throws
        // MissingMethodException), encode locally against this alphabet.
        // Decode is alphabet-agnostic on both runtimes — TinyCLR's
        // Convert.FromBase64String accepts both '!'/'*' and '+'/'/'.
        private static readonly char[] s_base64Rfc4648 = new char[] {
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
            'Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d','e','f',
            'g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v',
            'w','x','y','z','0','1','2','3','4','5','6','7','8','9','+','/'
        };

        private static string ToBase64Rfc4648(byte[] input) {
            var sb = new StringBuilder();
            var i = 0;
            while (i + 3 <= input.Length) {
                int b0 = input[i++], b1 = input[i++], b2 = input[i++];
                sb.Append(s_base64Rfc4648[b0 >> 2]);
                sb.Append(s_base64Rfc4648[((b0 & 0x3) << 4) | (b1 >> 4)]);
                sb.Append(s_base64Rfc4648[((b1 & 0xf) << 2) | (b2 >> 6)]);
                sb.Append(s_base64Rfc4648[b2 & 0x3f]);
            }
            var rem = input.Length - i;
            if (rem == 1) {
                int b0 = input[i];
                sb.Append(s_base64Rfc4648[b0 >> 2]);
                sb.Append(s_base64Rfc4648[(b0 & 0x3) << 4]);
                sb.Append("==");
            }
            else if (rem == 2) {
                int b0 = input[i], b1 = input[i + 1];
                sb.Append(s_base64Rfc4648[b0 >> 2]);
                sb.Append(s_base64Rfc4648[((b0 & 0x3) << 4) | (b1 >> 4)]);
                sb.Append(s_base64Rfc4648[(b1 & 0xf) << 2]);
                sb.Append('=');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sign the request string with a key.
        /// </summary>
        /// <param name="requestString">The request string input to sign.</param>
        /// <param name="key">The secret key used for encryption.</param>
        /// <returns>The signed request string.</returns>
        protected virtual string Sign(string requestString, string key) {
            var algorithm = new HMACSHA256(Convert.FromBase64String(key));
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(requestString));
            return ToBase64Rfc4648(hash);
        }

        private bool IsNullOrWhiteSpace(string s) {
            if (s == null)
                return true;

            if (s.IndexOf(" ") >= 0)
                return true;

            return false;
        }
    }
}
