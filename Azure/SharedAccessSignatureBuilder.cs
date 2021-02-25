//------------------------------------------------------------------------------ 
// Copyright (C) 2021 GHI Electronics
//
// This file is a modified version from Microsoft.
//
//------------------------------------------------------------------------------

using System;
using System.Text;
using GHIElectronics.TinyCLR.Cryptography;
using GHIElectronics.TinyCLR.Networking.Net;

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

        /// <summary>
        /// Sign the request string with a key.
        /// </summary>
        /// <param name="requestString">The request string input to sign.</param>
        /// <param name="key">The secret key used for encryption.</param>
        /// <returns>The signed request string.</returns>
        protected virtual string Sign(string requestString, string key) {
            var algorithm = new HMACSHA256(Convert.FromBase64String(key));

            var useRFC4648EncodingTemp = Convert.UseRFC4648Encoding;

            Convert.UseRFC4648Encoding = true;

            var sign = Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(requestString)));

            Convert.UseRFC4648Encoding = useRFC4648EncodingTemp; // restore what it was!

            return sign;
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
