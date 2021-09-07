using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

using GHIElectronics.TinyCLR.Cryptography;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0009 // Member access should be qualified.


namespace GHIElectronics.TinyCLR.Drivers.MemoryManager
{
    public interface IMemoryInterface
    {
        internal uint Size { get; }
        internal bool Deserialize(out Hashtable data, out uint usedMemory);
        internal bool Serialize(Hashtable data);
        internal void Dump(StringBuilder sb);
    }

    public abstract class MemoryInterfaceBase
    {
        protected readonly byte[] Sentinel = { 0x5a, 0xa5 };
        protected byte DataCountSize = sizeof(ushort); // default allows up to 65535 bytes to be serialized
        private const byte KeySize = sizeof(byte); // allows up to 256 keys
        private const byte TypeSize = sizeof(byte); // allows up to 256 data types to be defined
        protected byte DynamicTypeSize = sizeof(ushort); // default allows up to 65535 bytes for dynamically sized types (string/byte[])
        protected readonly Crc16 Crc = new();
        protected const byte CrcSize = sizeof(ushort);

        #region enum

        private enum DataType : byte
        {
            ///<summary> represents an <see cref="Array"/> of <see cref="byte"/> </summary>
            ByteArray,

            ///<summary> represents a <see cref="byte"/> </summary>
            Byte,

            ///<summary> represents an <see cref="sbyte"/> </summary>
            SByte,

            ///<summary> represents a <see cref="bool"/> </summary>
            Bool,

            ///<summary> represents a <see cref="short"/> </summary>
            Short,

            ///<summary> represents a <see cref="ushort"/> </summary>
            UShort,

            ///<summary> represents a <see cref="int"/> </summary>
            Int,

            ///<summary> represents a <see cref="uint"/> </summary>
            UInt,

            ///<summary> represents a <see cref="float"/> </summary>
            Float,

            ///<summary> represents a <see cref="long"/> </summary>
            Long,

            ///<summary> represents a <see cref="ulong"/> </summary>
            ULong,

            ///<summary> represents a <see cref="double"/> </summary>
            Double,

            ///<summary> represents a <see cref="char"/> </summary>
            Char,

            ///<summary> represents a <see cref="string"/> </summary>
            String
        }

        #endregion enum

        /// <summary> Encodes data according to its type </summary>
        /// <param name="entry"> Key value pairing of data </param>
        /// <param name="encoded"> Encoded data of <see cref="entry"/> </param>
        /// <returns> Length of encoded data </returns>
        protected ushort Encode(DictionaryEntry entry, out byte[] encoded)
        {
            var index = 0;
            encoded = entry.Value switch
            {
                byte[] bytes => EncodeDynamicType(bytes, DataType.ByteArray),
                byte @byte => EncodePrimitiveType(new[] { @byte }, DataType.Byte),
                sbyte @sbyte => EncodePrimitiveType(new[] { unchecked((byte)@sbyte) }, DataType.SByte),
                bool @bool => EncodePrimitiveType(BitConverter.GetBytes(@bool), DataType.Bool),
                short @short => EncodePrimitiveType(BitConverter.GetBytes(@short), DataType.Short),
                ushort @ushort => EncodePrimitiveType(BitConverter.GetBytes(@ushort), DataType.UShort),
                int @int => EncodePrimitiveType(BitConverter.GetBytes(@int), DataType.Int),
                uint @uint => EncodePrimitiveType(BitConverter.GetBytes(@uint), DataType.UInt),
                float @float => EncodePrimitiveType(BitConverter.GetBytes(@float), DataType.Float),
                long @long => EncodePrimitiveType(BitConverter.GetBytes(@long), DataType.Long),
                ulong @ulong => EncodePrimitiveType(BitConverter.GetBytes(@ulong), DataType.ULong),
                double @double => EncodePrimitiveType(BitConverter.GetBytes(@double), DataType.Double),
                char @char => EncodeDynamicType(BitConverter.GetBytes(@char), DataType.Char),
                string @string => EncodeDynamicType(Encoding.UTF8.GetBytes(@string), DataType.String),
                _ => throw new ArgumentOutOfRangeException($"{nameof(entry.Value)}", $"Unrecognized type to be encoded to memory stream: {entry.Value}")

            };
            return (ushort)encoded.Length;

            byte[] EncodeDynamicType(byte[] bytes, DataType dataType)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));

                var encode = new byte[KeySize + TypeSize + DynamicTypeSize + bytes.Length];
                encode[index] = (byte)entry.Key;
                encode[index += KeySize] = (byte)dataType;
                var dynamicSize = BitConverter.GetBytes((ushort)bytes.Length);
                Array.Copy(dynamicSize, 0, encode, index += TypeSize, dynamicSize.Length);
                Array.Copy(bytes, 0, encode, index + dynamicSize.Length, bytes.Length);
                return encode;
            }

            byte[] EncodePrimitiveType(byte[] bytes, DataType dataType)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));

                var encode = new byte[KeySize + TypeSize + bytes.Length];
                encode[index] = (byte)entry.Key;
                encode[index += KeySize] = (byte)dataType;
                Array.Copy(bytes, 0, encode, index + TypeSize, bytes.Length);
                return encode;
            }
        }

        /// <summary> Decode data stream </summary>
        /// <param name="data"> Data to decode </param>
        /// <param name="count"> Running count of individual data chunks in data stream </param>
        /// <returns> Individual data chunk value </returns>
        /// <exception cref="ArgumentNullException"> Thrown when <see cref="data"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown value read from memory stream is not a known data type <see cref="DataType"/>. </exception>
        protected object Decode(byte[] data, ref object count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var i = (ushort)count;
            var type = (DataType)data[i += KeySize];

            i += TypeSize; // move to data
            var decoded = type switch
            {
                DataType.ByteArray => ByteArrayType(),
                DataType.Byte => new object[] { data[i], sizeof(byte) },
                DataType.SByte => new object[] { unchecked((sbyte)data[i]), sizeof(sbyte) },
                DataType.Bool => new object[] { BitConverter.ToBoolean(data, i), sizeof(bool) },
                DataType.Short => new object[] { BitConverter.ToInt16(data, i), sizeof(short) },
                DataType.UShort => new object[] { BitConverter.ToUInt16(data, i), sizeof(ushort) },
                DataType.Int => new object[] { BitConverter.ToInt32(data, i), sizeof(int) },
                DataType.UInt => new object[] { BitConverter.ToUInt32(data, i), sizeof(uint) },
                DataType.Float => new object[] { BitConverter.ToSingle(data, i), sizeof(float) },
                DataType.Long => new object[] { BitConverter.ToInt64(data, i), sizeof(long) },
                DataType.ULong => new object[] { BitConverter.ToUInt64(data, i), sizeof(ulong) },
                DataType.Double => new object[] { BitConverter.ToDouble(data, i), sizeof(double) },
                DataType.Char => new object[] { BitConverter.ToChar(data, i += DynamicTypeSize), sizeof(char) },
                DataType.String => StringType(),
                _ => throw new ArgumentOutOfRangeException($"{nameof(data)}", $"Unrecognized type decoded in memory stream: {type}")

            };

            var value = decoded[0];
            i += (ushort)((int)(decoded[1])); // move to end

            count = i;
            return value;

            object[] ByteArrayType()
            {
                int dynamicTypeCount = BitConverter.ToUInt16(data, i);
                value = new byte[dynamicTypeCount];
                Array.Copy(data, i += DynamicTypeSize, (byte[])value, 0, dynamicTypeCount);
                return new[] { value, dynamicTypeCount };
            }

            object[] StringType()
            {
                int dynamicTypeCount = BitConverter.ToUInt16(data, i);
                return new object[] { Encoding.UTF8.GetString(data, i += DynamicTypeSize, dynamicTypeCount), dynamicTypeCount };
            }
        }

        /// <summary> Common header for printable memory dump </summary>
        /// <remarks> Used for debugging </remarks>
        /// <returns> string building used to dump memory </returns>
        protected static StringBuilder Dump(StringBuilder sb)
        {
            sb.Append("     ");
            for (var i = 0; i < 32; i++)
                sb.Append($"{i,0x03} ");
            sb.AppendLine();
            sb.Append("     ");
            for (var i = 0; i < 32; i++)
                sb.Append("----");
            Debug.WriteLine(sb.ToString());
            sb.Clear();
            return sb;
        }
    }
}
