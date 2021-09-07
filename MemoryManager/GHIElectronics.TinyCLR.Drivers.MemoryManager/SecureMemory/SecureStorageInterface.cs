using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using GHIElectronics.TinyCLR.Devices.SecureStorage;

// ReSharper disable TooWideLocalVariableScope
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0009 // Member access should be qualified.


namespace GHIElectronics.TinyCLR.Drivers.MemoryManager.SecureMemory
{
    public class SecureStorageInterface : MemoryInterfaceBase, IMemoryInterface
    {
        private readonly uint _blockCount;
        private readonly SecureStorageController _secureStorageController;

        public SecureStorageInterface(SecureStorageController secureStorageController)
        {
            _secureStorageController = secureStorageController;
            _blockCount = _secureStorageController.TotalSize / _secureStorageController.BlockSize; // 256

            if (((IMemoryInterface)this).Size > ushort.MaxValue)
                DataCountSize = DynamicTypeSize = sizeof(uint);
        }

        #region Implementation of IMemoryInterface

        uint IMemoryInterface.Size => _secureStorageController.TotalSize;

        /// <summary>
        /// Get structure of available data
        /// </summary>
        /// <returns> Structure of available data </returns>
        bool IMemoryInterface.Deserialize(out Hashtable data, out uint usedMemory)
        {
            data = new Hashtable();
            object count = (ushort)0;
            usedMemory = ((IMemoryInterface)this).Size; // assume full if memory is corrupt and cant be read

            // optimize by moving outside of loop (interpreter will not do this)
            var read = new byte[_secureStorageController.BlockSize];

            // seek data
            for (var blockIndex = (int)(_blockCount - 1); blockIndex >= 0; blockIndex--) // 255 -> 0
            {
                if (_secureStorageController.Read((uint)blockIndex, read) == _secureStorageController.BlockSize && BitConverter.ToUInt16(Sentinel, 0) == BitConverter.ToUInt16(read, 0))
                {
                    if (!Read((int)(blockIndex * _secureStorageController.BlockSize), out var dataBuffer))
                        return false;

                    while ((ushort)count < dataBuffer.Length)
                        data.Add(dataBuffer[(ushort)count], Decode(dataBuffer, ref count));

                    usedMemory = (ushort)count > 0 ? (uint)(Sentinel.Length + DataCountSize + dataBuffer.Length + CrcSize) : 0;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Serialize and write to memory
        /// </summary>
        /// <param name="data"> Data structure to serialize </param>
        /// <returns> True if resulting <see cref="data"/> was serialized and written to memory </returns>
        bool IMemoryInterface.Serialize(Hashtable data)
        {
            ushort dataCount = 0;
            var values = new ArrayList();
            foreach (DictionaryEntry entry in data)
            {
                dataCount += Encode(entry, out var output);
                values.Add(output);
            }

            // empty
            if (data.Count > 0)
                dataCount += CrcSize; // if any data add size of crc to count

            // too large for memory
            var size = ((IMemoryInterface)this).Size;
            if (dataCount > size - (Sentinel.Length - 1 + DataCountSize)) // lost capacity from not wrapping header
                return false;

            var headerSize = Sentinel.Length + DataCountSize;
            var encoded = new byte[headerSize + dataCount];
            Array.Copy(Sentinel, 0, encoded, 0, Sentinel.Length);
            var count = BitConverter.GetBytes(dataCount);
            Array.Copy(count, 0, encoded, Sentinel.Length, count.Length);

            // optimize by moving outside of loop (interpreter will not do this)
            byte[] bytes;

            var runningDataCount = headerSize;
            foreach (var value in values)
            {
                bytes = (byte[])value;
                Array.Copy(bytes, 0, encoded, runningDataCount, bytes.Length);
                runningDataCount += bytes.Length;
            }

            if (runningDataCount <= headerSize)
                return Write(encoded);

            Crc.Reset();
            var crc = BitConverter.GetBytes(Crc.ComputeHash(encoded, headerSize, encoded.Length - (headerSize + CrcSize)));
            Array.Copy(crc, 0, encoded, encoded.Length - CrcSize, crc.Length);

            return size >= encoded.Length && Write(encoded);
        }

        /// <summary> See entire memory area </summary>
        /// <remarks> For debugging </remarks>
        void IMemoryInterface.Dump(StringBuilder sb)
        {
            sb = Dump(sb);
            var block = new byte[_secureStorageController.BlockSize];
            for (uint i = 0; i < _blockCount; i++)
            {
                _secureStorageController.Read(i, block);
                sb.Append($"{i,0x03:x2}| ");
                for (var @byte = 0; @byte < 32; @byte++)
                    sb.Append($"{block[@byte],0x03:x2} ");
                Debug.WriteLine(sb.ToString());
                sb.Clear();
            }

            Debug.WriteLine(sb.ToString());
        }

        #endregion Implementation of IMemoryInterface

        ///  <summary>
        ///  Return only data from full data frame
        ///  </summary>
        /// <remarks> Points to frame start </remarks>
        ///  <param name="index"> Index of start of data frame in overall memory map</param>
        ///  <param name="data"> Read data </param>
        ///  <returns> True if data is read successfully </returns>
        private bool Read(int index, out byte[] data)
        {
            var blockIndex = GetBlockIndex(index);
            var byteIndex = GetByteIndex(index);

            data = new byte[0];

            // move index to count, get count
            //byteIndex += Sentinel.Length;
            if ((byteIndex += Sentinel.Length) >= _secureStorageController.BlockSize)
            {
                ++blockIndex;
                byteIndex = 0;
            }

            var read = new byte[_secureStorageController.BlockSize];
            _secureStorageController.Read((uint)blockIndex, read);

            // checking this here as any theoretical corruption in count can be recovered by redundancy
            // data is larger than can fit into memory
            var dataCount = BitConverter.ToUInt16(read, byteIndex);
            if (dataCount > _secureStorageController.TotalSize - Sentinel.Length - DataCountSize)
                return false;

            // create result array
            var buffer = new byte[dataCount];

            // move index to data
            if ((byteIndex += DataCountSize) >= _secureStorageController.BlockSize)
            {
                ++blockIndex;
                byteIndex = 0;
            }

            // gather data
            var remainingCount = (int)dataCount;
            var destinationIndex = 0;

            // optimize by moving outside of loop (interpreter will not do this)
            int segmentLength;
            int length;

            while (remainingCount > 0)
            {
                _secureStorageController.Read((uint)blockIndex, read);
                segmentLength = (int)(_secureStorageController.BlockSize - byteIndex);
                length = remainingCount < segmentLength ? remainingCount : segmentLength;
                Array.Copy(read, byteIndex, buffer, destinationIndex, length);
                remainingCount -= length;
                destinationIndex += length;
                ++blockIndex;
                byteIndex = 0;
            }

            if (dataCount > 0)
            {
                var dataSize = buffer.Length - CrcSize;
                data = new byte[dataSize];
                Array.Copy(buffer, 0, data, 0, data.Length);

                Crc.Reset();
                return Crc.ComputeHash(buffer, 0, dataSize) == BitConverter.ToUInt16(buffer, dataSize);
            }

            return true;
        }

        /// <summary>
        /// Write 1 full data frame (Sentinel to CRC) to memory<para />
        /// Api limits writing to each block only once until erased, therefore all writes start at block index 0
        /// </summary>
        /// <remarks> Memory read in order to find end of data </remarks>
        /// <param name="data"> Full data frame </param>
        /// <returns> True if data was verified to be written at least once </returns>
        /// <exception cref="ArgumentNullException"> Thrown if <see cref="data"/> is null </exception>
        private bool Write(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var index = -1;

            // optimize by moving outside of loop (interpreter will not do this)
            var read = new byte[_secureStorageController.BlockSize];

            // seek data end
            for (var blockIndex = (int)(_blockCount - 1); blockIndex >= 0; blockIndex--) // 255 -> 0
            {
                // bad block
                if (_secureStorageController.Read((uint)blockIndex, read) != _secureStorageController.BlockSize)
                    return false;

                var empty = true;
                for (var byteIndex = (int)_secureStorageController.BlockSize - 1; byteIndex >= 0; byteIndex--) // 31 -> 0
                {
                    if (read[byteIndex] != 0xff)
                    {
                        index = (int)(++blockIndex * _secureStorageController.BlockSize); // secure storage blocks can only be written to once before requiring to be erased so go to next block
                        empty = false;
                        break;
                    }
                }

                if (!empty)
                    break;
            }

            // uninitialized
            if (index < 0)
                index = 0;

            // full, will overflow
            var full = index >= _secureStorageController.TotalSize;
            var free = (int)_secureStorageController.TotalSize - index;
            var needed = data.Length;
            if (full || needed > free)
            {
                Erase();
                index = 0;
            }

            // write and validate
            return this.CoreWrite(data, index) && Read(index, out _);
        }

        /// <summary> Write entire 1 full or partial block to memory </summary>
        /// <remarks> Expects 1 full frame (Sentinel+Count+Data+Crc) </remarks>
        /// <param name="data"> Full or partial block of memory to write </param>
        /// <param name="index"> Start index </param>
        /// <exception cref="ArgumentNullException"> Thrown if <see cref="data"/> is null </exception>
        private bool CoreWrite(byte[] data, int index)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var writeBuffer = new byte[_secureStorageController.BlockSize];
            var remainingLength = data.Length;
            var count = 0;

            // optimize by moving outside of loop (interpreter will not do this)
            int blockIndex;
            int byteIndex;
            int remainingBlockLength;
            int writeLength;

            while (remainingLength > 0)
            {
                blockIndex = GetBlockIndex(index);
                byteIndex = GetByteIndex(index);
                remainingBlockLength = (int)(_secureStorageController.BlockSize - byteIndex);
                writeLength = remainingLength < remainingBlockLength ? remainingLength : remainingBlockLength;

                // empty, set, write
                for (var i = 0; i < _secureStorageController.BlockSize; i++)
                    writeBuffer[i] = byte.MaxValue;

                Array.Copy(data, count, writeBuffer, byteIndex, writeLength);

                if (_secureStorageController.Write((uint)blockIndex, writeBuffer) != _secureStorageController.BlockSize)
                    return false; // write failed ToDo anything? go to next byte? block? throw? log?

                // update indices
                index += writeLength;
                count += writeLength;
                remainingLength -= writeLength;
            }

            return data.Length == count;
        }

        /// <summary> Erase Entire memory area if not already blank </summary>
        private void Erase()
        {
            if (!IsAllBlank())
                _secureStorageController.Erase();
        }

        /// <summary>
        /// Check entire memory area is blank (erased)
        /// </summary>
        /// <returns></returns>
        private bool IsAllBlank()
        {
            for (uint block = 0; block < _blockCount; block++)
                if (!_secureStorageController.IsBlank(block))
                    return false;

            return true;
        }

        private int GetBlockIndex(int index) => (int)(index / _secureStorageController.BlockSize);

        private int GetByteIndex(int index) => (int)(index % _secureStorageController.BlockSize);
    }
}
