/*
* Copyright 2007 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;

namespace GHIElectronics.TinyCLR.Drivers.Barcode.Common
{
   /// <summary> <p>A simple, fast array of bits, represented compactly by an array of ints internally.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public sealed class BitArray
   {
      private int[] bits;
      private int size;

        public int Size => this.size;

        public int SizeInBytes => (this.size + 7) >> 3;

        public bool this[int i] {
            get => (this.bits[i >> 5] & (1 << (i & 0x1F))) != 0;
            set {
                if (value)
                    this.bits[i >> 5] |= 1 << (i & 0x1F);
            }
        }

        public BitArray()
      {
         this.size = 0;
         this.bits = new int[1];
      }

      public BitArray(int size)
      {
         if (size < 1)
         {
            throw new ArgumentException("size must be at least 1");
         }
         this.size = size;
         this.bits = makeArray(size);
      }

      private void ensureCapacity(int size)
      {
         if (size > this.bits.Length << 5)
         {
            int[] newBits = makeArray(size);
            System.Array.Copy(this.bits, 0, newBits, 0, this.bits.Length);
                this.bits = newBits;
         }
      }

      /// <summary> Flips bit i.
      /// 
      /// </summary>
      /// <param name="i">bit to set
      /// </param>
      public void flip(int i)
      {
            this.bits[i >> 5] ^= 1 << (i & 0x1F);
      }

      private static int numberOfTrailingZeros(int num)
      {
         var index = (-num & num)%37;
         if (index < 0)
            index *= -1;
         return _lookup[index];
      }

      private static readonly int[] _lookup =
         {
            32, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4, 7, 17,
            0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5, 20, 8, 19, 18
         };

      /// <summary>
      /// Gets the next set.
      /// </summary>
      /// <param name="from">first bit to check</param>
      /// <returns>index of first bit that is set, starting from the given index, or size if none are set
      /// at or beyond this given index</returns>
      public int getNextSet(int from)
      {
         if (from >= this.size)
         {
            return this.size;
         }
         int bitsOffset = from >> 5;
         int currentBits = this.bits[bitsOffset];
         // mask off lesser bits first
         currentBits &= ~((1 << (from & 0x1F)) - 1);
         while (currentBits == 0)
         {
            if (++bitsOffset == this.bits.Length)
            {
               return this.size;
            }
            currentBits = this.bits[bitsOffset];
         }
         int result = (bitsOffset << 5) + numberOfTrailingZeros(currentBits);
         return result > this.size ? this.size : result;
      }

      /// <summary>
      /// see getNextSet(int)
      /// </summary>
      /// <param name="from"></param>
      /// <returns></returns>
      public int getNextUnset(int from)
      {
         if (from >= this.size)
         {
            return this.size;
         }
         int bitsOffset = from >> 5;
         int currentBits = ~this.bits[bitsOffset];
         // mask off lesser bits first
         currentBits &= ~((1 << (from & 0x1F)) - 1);
         while (currentBits == 0)
         {
            if (++bitsOffset == this.bits.Length)
            {
               return this.size;
            }
            currentBits = ~this.bits[bitsOffset];
         }
         int result = (bitsOffset << 5) + numberOfTrailingZeros(currentBits);
         return result > this.size ? this.size : result;
      }

      /// <summary> Sets a block of 32 bits, starting at bit i.
      /// 
      /// </summary>
      /// <param name="i">first bit to set
      /// </param>
      /// <param name="newBits">the new value of the next 32 bits. Note again that the least-significant bit
      /// corresponds to bit i, the next-least-significant to i+1, and so on.
      /// </param>
      public void setBulk(int i, int newBits)
      {
            this.bits[i >> 5] = newBits;
      }

      /// <summary>
      /// Sets a range of bits.
      /// </summary>
      /// <param name="start">start of range, inclusive.</param>
      /// <param name="end">end of range, exclusive</param>
      public void setRange(int start, int end)
      {
         if (end < start)
         {
            throw new ArgumentException();
         }
         if (end == start)
         {
            return;
         }
         end--; // will be easier to treat this as the last actually set bit -- inclusive
         int firstInt = start >> 5;
         int lastInt = end >> 5;
         for (int i = firstInt; i <= lastInt; i++)
         {
            int firstBit = i > firstInt ? 0 : start & 0x1F;
            int lastBit = i < lastInt ? 31 : end & 0x1F;
            int mask;
            if (firstBit == 0 && lastBit == 31)
            {
               mask = -1;
            }
            else
            {
               mask = 0;
               for (int j = firstBit; j <= lastBit; j++)
               {
                  mask |= 1 << j;
               }
            }
                this.bits[i] |= mask;
         }
      }

      /// <summary> Clears all bits (sets to false).</summary>
      public void clear()
      {
         int max = this.bits.Length;
         for (int i = 0; i < max; i++)
         {
                this.bits[i] = 0;
         }
      }

      /// <summary> Efficient method to check if a range of bits is set, or not set.
      /// 
      /// </summary>
      /// <param name="start">start of range, inclusive.
      /// </param>
      /// <param name="end">end of range, exclusive
      /// </param>
      /// <param name="value">if true, checks that bits in range are set, otherwise checks that they are not set
      /// </param>
      /// <returns> true iff all bits are set or not set in range, according to value argument
      /// </returns>
      /// <throws>  IllegalArgumentException if end is less than or equal to start </throws>
      public bool isRange(int start, int end, bool value)
      {
         if (end < start)
         {
            throw new System.ArgumentException();
         }
         if (end == start)
         {
            return true; // empty range matches
         }
         end--; // will be easier to treat this as the last actually set bit -- inclusive    
         int firstInt = start >> 5;
         int lastInt = end >> 5;
         for (int i = firstInt; i <= lastInt; i++)
         {
            int firstBit = i > firstInt ? 0 : start & 0x1F;
            int lastBit = i < lastInt ? 31 : end & 0x1F;
            int mask;
            if (firstBit == 0 && lastBit == 31)
            {
               mask = -1;
            }
            else
            {
               mask = 0;
               for (int j = firstBit; j <= lastBit; j++)
               {
                  mask |= 1 << j;
               }
            }

            // Return false if we're looking for 1s and the masked bits[i] isn't all 1s (that is,
            // equals the mask, or we're looking for 0s and the masked portion is not all 0s
            if ((this.bits[i] & mask) != (value ? mask : 0))
            {
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Appends the bit.
      /// </summary>
      /// <param name="bit">The bit.</param>
      public void appendBit(bool bit)
      {
            this.ensureCapacity(this.size + 1);
         if (bit)
         {
                this.bits[this.size >> 5] |= 1 << (this.size & 0x1F);
         }
            this.size++;
      }

        /// <returns> underlying array of ints. The first element holds the first 32 bits, and the least
        /// significant bit is bit 0.
        /// </returns>
        public int[] Array => this.bits;

        /// <summary>
        /// Appends the least-significant bits, from value, in order from most-significant to
        /// least-significant. For example, appending 6 bits from 0x000001E will append the bits
        /// 0, 1, 1, 1, 1, 0 in that order.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="numBits">The num bits.</param>
        public void appendBits(int value, int numBits)
      {
         if (numBits < 0 || numBits > 32)
         {
            throw new ArgumentException("Num bits must be between 0 and 32");
         }
            this.ensureCapacity(this.size + numBits);
         for (int numBitsLeft = numBits; numBitsLeft > 0; numBitsLeft--)
         {
                this.appendBit(((value >> (numBitsLeft - 1)) & 0x01) == 1);
         }
      }

      public void appendBitArray(BitArray other)
      {
         int otherSize = other.size;
            this.ensureCapacity(this.size + otherSize);
         for (int i = 0; i < otherSize; i++)
         {
                this.appendBit(other[i]);
         }
      }

      public void xor(BitArray other)
      {
         if (this.bits.Length != other.bits.Length)
         {
            throw new ArgumentException("Sizes don't match");
         }
         for (int i = 0; i < this.bits.Length; i++)
         {
                // The last byte could be incomplete (i.e. not have 8 bits in
                // it) but there is no problem since 0 XOR 0 == 0.
                this.bits[i] ^= other.bits[i];
         }
      }

      /// <summary>
      /// Toes the bytes.
      /// </summary>
      /// <param name="bitOffset">first bit to start writing</param>
      /// <param name="array">array to write into. Bytes are written most-significant byte first. This is the opposite
      /// of the internal representation, which is exposed by BitArray</param>
      /// <param name="offset">position in array to start writing</param>
      /// <param name="numBytes">how many bytes to write</param>
      public void toBytes(int bitOffset, byte[] array, int offset, int numBytes)
      {
         for (int i = 0; i < numBytes; i++)
         {
            int theByte = 0;
            for (int j = 0; j < 8; j++)
            {
               if (this[bitOffset])
               {
                  theByte |= 1 << (7 - j);
               }
               bitOffset++;
            }
            array[offset + i] = (byte)theByte;
         }
      }

      /// <summary> Reverses all bits in the array.</summary>
      public void reverse()
      {
         int[] newBits = new int[this.bits.Length];
         int size = this.size;
         for (int i = 0; i < size; i++)
         {
            if (this[size - i - 1])
            {
               newBits[i >> 5] |= 1 << (i & 0x1F);
            }
         }
            this.bits = newBits;
      }

      private static int[] makeArray(int size)
      {
         return new int[(size + 31) >> 5];
      }

      public override string ToString()
      {
         var result = new System.Text.StringBuilder(this.size);
         for (int i = 0; i < this.size; i++)
         {
            if ((i & 0x07) == 0)
            {
               result.Append(' ');
            }
            result.Append(this[i] ? 'X' : '.');
         }
         return result.ToString();
      }
   }
}
