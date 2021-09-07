using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using GHIElectronics.TinyCLR.Devices.Rtc;

// ReSharper disable TooWideLocalVariableScope
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0009 // Member access should be qualified.


namespace GHIElectronics.TinyCLR.Drivers.MemoryManager.RtcMemory
{
    public class RtcMemoryInterface : MemoryInterfaceBase, IMemoryInterface
    {
        private readonly RtcController _rtcControllerProvider;
        private int _dataPointer = -1;

        public RtcMemoryInterface(RtcController rtcControllerProvider)
        {
            _rtcControllerProvider = rtcControllerProvider;

            if (((IMemoryInterface)this).Size > ushort.MaxValue)
                DataCountSize = DynamicTypeSize = sizeof(uint);
        }

        #region Implementation of IMemoryInterface

        uint IMemoryInterface.Size => _rtcControllerProvider.BackupMemorySize;

        /// <summary>
        /// Get structure of available data
        /// </summary>
        /// <returns> Structure of available data</returns>
        bool IMemoryInterface.Deserialize(out Hashtable data, out uint usedMemory)
        {
            data = new Hashtable();
            object count = (ushort)0;
            usedMemory = 0;

            if (_dataPointer >= 0)
            {
                if (!Read(_dataPointer, out var dataBuffer))
                    return false;

                while ((ushort)count < dataBuffer.Length)
                    data.Add(dataBuffer[(ushort)count], Decode(dataBuffer, ref count));

                usedMemory = (ushort)count > 0 ? (uint)(DataCountSize + dataBuffer.Length + CrcSize) : 0;
            }

            return true;
        }

        /// <summary>
        /// Serialize and write to memory
        /// </summary>
        /// <param name="data"> Data structure to serialize </param>
        /// <returns> True if resulting <see cref="data"/> was serialized and written to memory</returns>
        bool IMemoryInterface.Serialize(Hashtable data)
        {
            ushort dataSize = 0;
            var values = new ArrayList();
            foreach (DictionaryEntry entry in data)
            {
                dataSize += Encode(entry, out var output);
                values.Add(output);
            }

            // empty
            if (data.Count == 0)
            {
                _dataPointer = -1;
                return true;
            }

            int headerSize = DataCountSize;
            var payloadSize = dataSize + CrcSize;
            var totalSize = headerSize + payloadSize;

            // too large for memory
            if (totalSize > ((IMemoryInterface)this).Size)
                return false;

            var encoded = new byte[totalSize];
            var count = BitConverter.GetBytes(payloadSize);
            Array.Copy(count, 0, encoded, 0, count.Length);

            var runningDataCount = headerSize;

            // optimize by moving outside of loop (interpreter will not do this)
            byte[] bytes;

            foreach (var value in values)
            {
                bytes = (byte[])value;
                Array.Copy(bytes, 0, encoded, runningDataCount, bytes.Length);
                runningDataCount += bytes.Length;
            }

            Crc.Reset();
            var crc = BitConverter.GetBytes(Crc.ComputeHash(encoded, headerSize, dataSize));
            Array.Copy(crc, 0, encoded, encoded.Length - CrcSize, crc.Length);

            return Write(encoded);
        }

        /// <summary> See entire memory area </summary>
        /// <remarks> For debugging </remarks>
        void IMemoryInterface.Dump(StringBuilder sb)
        {
            sb = Dump(sb);
            var block = new byte[32];
            for (uint i = 0; i < _rtcControllerProvider.BackupMemorySize / 32; i++)
            {
                _rtcControllerProvider.ReadBackupMemory(block, 0, i * 32, 32);
                sb.Append($"{i,0x03:x2}| ");
                for (var @byte = 0; @byte < 32; @byte++)
                    sb.Append($"{block[@byte],0x03:x2} ");
                sb.AppendLine();
            }

            Debug.WriteLine(sb.ToString());
        }

        #endregion Implementation of IMemoryInterface

        ///  <summary>
        ///  Return only data from full frame
        ///  </summary>
        /// <remarks> Points to frame start </remarks>
        ///  <param name="index"> Index of start of data frame in overall memory </param>
        ///  <param name="data"> Read memory </param>
        ///  <returns> True if memory is read successfully </returns>
        private bool Read(int index, out byte[] data)
        {
            data = new byte[0];

            // test bounds
            var size = ((IMemoryInterface)this).Size;
            if (index >= size)
                index = 0;

            // gather/check count
            if (!CoreRead(out var countGather, DataCountSize, ref index))
                return false;

            var dataCount = BitConverter.ToUInt16(countGather, 0);
            if (dataCount > size - DataCountSize)
                return false;

            if (dataCount <= 0)
                return true;

            // gather/check data
            if (!CoreRead(out var gathered, dataCount, ref index))
                return false;

            var dataSize = gathered.Length - CrcSize;
            data = new byte[dataSize];
            Array.Copy(gathered, 0, data, 0, data.Length);

            Crc.Reset();
            return Crc.ComputeHash(gathered, 0, dataSize) == BitConverter.ToUInt16(gathered, dataSize);
        }

        private bool CoreRead(out byte[] gathered, int count, ref int index)
        {
            gathered = new byte[count];
            var destinationOffset = 0;

            // optimize by moving outside of loop (interpreter will not do this)
            int remainingBlockLength;
            int readLength;
            var size = ((IMemoryInterface)this).Size;

            while (count > 0)
            {
                remainingBlockLength = (int)(size - index);
                readLength = count < remainingBlockLength ? count : remainingBlockLength;

                if (_rtcControllerProvider.ReadBackupMemory(gathered, (uint)destinationOffset, (uint)index, readLength) != readLength)
                    return false;

                if ((index += readLength) >= size)
                    index = 0;
                destinationOffset += readLength;
                count -= readLength;
            }

            return true;
        }

        /// <summary>
        /// Write data to memory <para/>
        /// </summary>
        /// <param name="data"> Full data frame </param>
        /// <param name="index"> Optional index to begin write </param>
        /// <returns> True if data was verified to be written </returns>
        /// <exception cref="ArgumentNullException"> Thrown if <see cref="data"/> is null </exception>
        private bool Write(byte[] data, int index = -1)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var size = ((IMemoryInterface)this).Size;
            index = index < 0 || index >= size ? new Random().Next((int)size) : index;
            var temp = index;

            // write and validate
            if (!CoreWrite(data, index) || !Read(index, out _))
                return false;

            _dataPointer = temp;
            return true;
        }

        /// <summary> Write Expects 1 full frame (Count+Data+Crc) to memory </summary>
        /// <param name="data"> Full or partial block of memory to write </param>
        /// <param name="index"> Start index </param>
        private bool CoreWrite(byte[] data, int index)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var remainingLength = data.Length;
            var count = 0;

            // optimize by moving outside of loop (interpreter will not do this)
            int remainingBlockLength;
            int writeLength;
            var size = ((IMemoryInterface)this).Size;

            while (remainingLength > 0)
            {
                remainingBlockLength = (int)(size - index);
                writeLength = remainingLength < remainingBlockLength ? remainingLength : remainingBlockLength;

                _rtcControllerProvider.WriteBackupMemory(data, (uint)count, (uint)index, writeLength);

                // update indices
                if ((index += writeLength) >= size)
                    index = 0;
                count += writeLength;
                remainingLength -= writeLength;
            }

            return data.Length == count;
        }
    }
}
