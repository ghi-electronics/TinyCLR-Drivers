using System;
using System.Text;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0009 // Member access should be qualified.


namespace GHIElectronics.TinyCLR.Drivers.MemoryManager
{
    public class MemoryManager
    {
        private readonly IMemoryInterface _memoryInterface;

        /// <summary> Instantiate a new copy of the <see cref="MemoryManager"/> </summary>
        /// <param name="memoryInterface"></param>
        public MemoryManager(IMemoryInterface memoryInterface) => _memoryInterface = memoryInterface;

        /// <summary> Recall a value </summary>
        /// <param name="key"> Item to recall </param>
        /// <param name="value"> Value of item </param>
        /// <returns> True if item was found </returns>
        public bool Recall(byte key, out object value)
        {
            value = null;
            if (_memoryInterface.Deserialize(out var data, out _) && data.Contains(key))
            {
                value = data[key];
                return true;
            }

            return false;
        }

        /// <summary> Adds or Replaces a value in the secure memory manager </summary>
        /// <param name="key"> Item to remove or replace </param>
        /// <param name="value"> Value of item </param>
        /// <returns> True if item was added or replaced </returns>
        /// <exception cref="ArgumentNullException"> Thrown when <see cref="value"/> is null </exception>
        public bool AddOrReplace(byte key, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (_memoryInterface.Deserialize(out var data, out _) && data.Contains(key))
                data.Remove(key);

            data.Add(key, value);
            return _memoryInterface.Serialize(data);
        }

        /// <summary> Remove a value from the secure memory manager </summary>
        /// <param name="key"> Item to remove </param>
        /// <returns> True if item was found and removed </returns>
        public bool Remove(byte key)
        {
            if (_memoryInterface.Deserialize(out var data, out _) && data.Contains(key))
            {
                data.Remove(key);
                if (_memoryInterface.Serialize(data))
                    return true;
            }

            return false;
        }

        /// <summary> Retrieve the size of free memory </summary>
        /// <returns> Free memory in bytes</returns>
        public uint Free()
        {
            _memoryInterface.Deserialize(out _, out var used);
            return _memoryInterface.Size - used;
        }

        public void Dump() => _memoryInterface.Dump(new StringBuilder($"{_memoryInterface.GetType().Name} memory map:{Environment.NewLine}"));
    }
}
