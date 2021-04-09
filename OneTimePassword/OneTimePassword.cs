using System;
using System.Collections;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Cryptography;

namespace GHIElectronics.TinyCLR.Drivers.OneTimePassword {
    public class OneTimePassword {

        private readonly int[] digitsPower = { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000 };

        private const long EPOCH = 621355968000000000;

        private int digits = 6;
        public int Length {
            get => this.digits;
            set {
                if (value < 1 || value >= this.digitsPower.Length)
                    throw new ArgumentException("Password length must be between 1 to 9.");
                this.digits = value;
            }
        }

        private byte[] bKey;

        public OneTimePassword(string key) {
            if (key == null) {
                throw new ArgumentNullException("Key null.");
            }

            if (key.Length < 1 || key.Length > 64)
                throw new ArgumentException("key length must be between 1 to 64.");

            this.bKey = this.ToBytesBase32(key);
        }

        private byte[] HmacSha1(byte[] data) {
            var hmac = new HMACSHA1(this.bKey);
            var result = hmac.ComputeHash(data);

            return result;
        }

        public string Generate(int token) => throw new NotSupportedException();
        public string Generate(long utcTick, TimeSpan range) {

            var timeSource = (utcTick - EPOCH) / TimeSpan.TicksPerMillisecond;
            var interval = (int)range.TotalMilliseconds;

            var code = new byte[8];

            if (interval != 0)
                code = BitConverter.GetBytes(timeSource / interval);

            if (BitConverter.IsLittleEndian) {
                code = this.Reverse(code);
            }

            var hash = this.HmacSha1(code);

            var offset = hash[hash.Length - 1] & 0xf;

            var binary =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            var otp = binary % this.digitsPower[this.digits];

            var result = otp.ToString();

            while (result.Length < this.digits) {
                result = "0" + result;
            }
            return result;
        }

        private byte[] Reverse(byte[] source) {
            var reversed = new byte[source.Length];

            for (var i = 0; i < source.Length; i++) {
                reversed[i] = source[source.Length - 1 - i];
            }

            return reversed;
        }

        private byte[] ToBytesBase32(string source) {
            source = source.TrimEnd('=');
            var byteCount = source.Length * 5 / 8;
            var returnArray = new byte[byteCount];

            byte curByte = 0, bitsRemaining = 8;
            int mask, arrayIndex = 0;

            var chars = source.ToUpper().ToCharArray();
            for (var i = 0; i < chars.Length; i++) {
                var cValue = this.CharToValue(chars[i]);

                if (bitsRemaining > 5) {
                    mask = cValue << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else {
                    mask = cValue >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(cValue << (3 + bitsRemaining));
                    bitsRemaining += 3;
                }
            }

            if (arrayIndex != byteCount) {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }

        private int CharToValue(int c) {
            if (c < 91 && c > 64) {
                return c - 65;
            }
            if (c < 56 && c > 49) {
                return c - 24;
            }
            if (c < 123 && c > 96) {
                return c - 97;
            }

            return -1;
        }
    }
}
