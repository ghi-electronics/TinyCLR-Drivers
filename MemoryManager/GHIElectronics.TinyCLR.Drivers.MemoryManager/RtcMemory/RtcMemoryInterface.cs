using System;
using System.Collections;
using System.Diagnostics;

using GHIElectronics.TinyCLR.Devices.Rtc;

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
			this._rtcControllerProvider = rtcControllerProvider;

			if (((IMemoryInterface)this).Size > ushort.MaxValue)
				DataCountSize = DynamicTypeSize = sizeof(uint);
		}

		#region Implementation of IMemoryInterface

		uint IMemoryInterface.Size => this._rtcControllerProvider.BackupMemorySize;

		/// <summary>
		/// Get structure of available data
		/// </summary>
		/// <returns> Structure of available data</returns>
		/// <exception cref="InvalidOperationException"> Thrown when a block of memory can't be read. </exception>
		Hashtable IMemoryInterface.Deserialize(out uint usedMemory)
		{
			var data = new Hashtable();
			object count = (ushort)0;

			if (this._dataPointer >= 0)
			{
				if (this.Read(this._dataPointer, out var dataBuffer))
				{
					while ((ushort)count < dataBuffer.Length)
						data.Add(dataBuffer[(ushort)count], this.Decode(dataBuffer, ref count));

					usedMemory = (ushort)count > 0 ? (uint)(this.DataCountSize + dataBuffer.Length + CrcSize) : 0;
					return data;
				}
			}

			this._dataPointer = -1;
			usedMemory = 0;
			return data;
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
				dataSize += this.Encode(entry, out var output);
				values.Add(output);
			}

			// empty
			if (data.Count == 0)
			{
				this._dataPointer = -1;
				return true;
			}

			int headerSize = this.DataCountSize;
			var payloadSize = dataSize + CrcSize;
			var totalSize = headerSize + payloadSize;

			// too large for memory
			if (totalSize > ((IMemoryInterface)this).Size)
				return false;

			var encoded = new byte[totalSize];
			var count = BitConverter.GetBytes(payloadSize);
			Array.Copy(count, 0, encoded, 0, count.Length);

			var runningDataCount = headerSize;
			foreach (var value in values)
			{
				var bytes = (byte[])value;
				Array.Copy(bytes, 0, encoded, runningDataCount, bytes.Length);
				runningDataCount += bytes.Length;
			}

			this.Crc.Reset();
			var crc = BitConverter.GetBytes(this.Crc.ComputeHash(encoded, headerSize, dataSize));
			Array.Copy(crc, 0, encoded, encoded.Length - CrcSize, crc.Length);

			return this.Write(encoded);
		}

		/// <summary> Get entire memory area </summary>
		/// <remarks> For debugging </remarks>
		/// <returns> Entire memory area</returns>
		void IMemoryInterface.Dump()
		{
			var sb = Dump();

			var block = new byte[32];
			for (uint i = 0; i < this._rtcControllerProvider.BackupMemorySize / 32; i++) {
				this._rtcControllerProvider.ReadBackupMemory(block, 0, i * 32, 32);
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
		///  <param name="index"> Index of start of data frame in overall memory map </param>
		///  <param name="data"></param>
		///  <returns> True if memory is read successfully </returns>
		private bool Read(int index, out byte[] data)
		{
			data = new byte[0];

			// test bounds
			if (index >= ((IMemoryInterface)this).Size)
				index = 0;

			// gather/check count
			if (!this.Gather(out var countGather, this.DataCountSize, ref index))
				return false;

			var dataCount = BitConverter.ToUInt16(countGather, 0);
			if (dataCount > ((IMemoryInterface)this).Size - this.DataCountSize)
				return false;

			if (dataCount <= 0)
				return true;

			// gather/check data
			if (!this.Gather(out var gathered, dataCount, ref index))
				return false;

			var dataSize = gathered.Length - CrcSize;
			data = new byte[dataSize];
			Array.Copy(gathered, 0, data, 0, data.Length);

			this.Crc.Reset();
			return this.Crc.ComputeHash(gathered, 0, dataSize) == BitConverter.ToUInt16(gathered, dataSize);
		}

		private bool Gather(out byte[] gathered, int count, ref int index)
		{
			gathered = new byte[count];
			var destinationOffset = 0;
			while (count > 0)
			{
				var remainingBlockLength = (int)(((IMemoryInterface)this).Size - index);
				var readLength = count < remainingBlockLength ? count : remainingBlockLength;

				if (this._rtcControllerProvider.ReadBackupMemory(gathered, (uint)destinationOffset, (uint)index, readLength) != readLength)
					return false;

				if ((index += readLength) >= ((IMemoryInterface)this).Size)
					index = 0;
				destinationOffset += readLength;
				count -= readLength;
			}

			return true;
		}

		/// <summary>
		/// Write data full frame to memory, with as many redundant copies as specified <para/>
		/// </summary>
		/// <param name="data"> Full data frame </param>
		/// <returns> True if data was verified to be written at least once </returns>
		/// <exception cref="InvalidOperationException"> Thrown when a block of memory cant be read. </exception>
		/// <remarks> Memory read in order to find end of data </remarks>
		private bool Write(byte[] data, int index = -1)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			index = index < 0 ? new Random().Next((int)((IMemoryInterface)this).Size): index;
			var temp = index;

			// write and validate
			if (this.CoreWrite(data, index) && this.Read(index, out _))
			{
				this._dataPointer = temp;
				return true;
			}

			// debug
			//var error = new byte[] { 0xee };
			//_rtcControllerProvider.WriteBackupMemory(error, ?);

			return false;
		}

		/// <summary> Write Expects 1 full frame (Count+Data+Crc) to memory </summary>
		/// <param name="data"> Full or partial block of memory to write</param>
		/// <param name="index"> Start index </param>
		/// <remarks> Wraps. No offsets. </remarks>
		private bool CoreWrite(byte[] data, int index)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			var remainingLength = data.Length;
			var count = 0;

			while (remainingLength > 0)
			{
				var remainingBlockLength = (int)(((IMemoryInterface)this).Size - index);
				var writeLength = remainingLength < remainingBlockLength ? remainingLength : remainingBlockLength;

				this._rtcControllerProvider.WriteBackupMemory(data, (uint)count, (uint)index, writeLength);

				// update indices
				if ((index += writeLength) >= ((IMemoryInterface)this).Size)
					index = 0;
				count += writeLength;
				remainingLength -= writeLength;
			}

			return data.Length == count;
		}
	}
}
