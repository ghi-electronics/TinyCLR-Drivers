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
   /// <summary>
   /// <p>Represents a 2D matrix of bits. In function arguments below, and throughout the common
   /// module, x is the column position, and y is the row position. The ordering is always x, y.
   /// The origin is at the top-left.</p>
   /// 
   /// <p>Internally the bits are represented in a 1-D array of 32-bit ints. However, each row begins
   /// with a new int. This is done intentionally so that we can copy out a row into a BitArray very
   /// efficiently.</p>
   /// 
   /// <p>The ordering of bits is row-major. Within each int, the least significant bits are used first,
   /// meaning they represent lower x values. This is compatible with BitArray's implementation.</p>
   /// 
   /// </summary>
   /// <author>  Sean Owen
   /// </author>
   /// <author>  dswitkin@google.com (Daniel Switkin)
   /// </author>
   /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
   /// </author>
   public sealed partial class BitMatrix
   {
      private readonly int width;
      private readonly int height;
      private readonly int rowSize;
      private readonly int[] bits;

        /// <returns> The width of the matrix
        /// </returns>
        public int Width => this.width;
        /// <returns> The height of the matrix
        /// </returns>
        public int Height => this.height;
        /// <summary> This method is for compatibility with older code. It's only logical to call if the matrix
        /// is square, so I'm throwing if that's not the case.
        /// 
        /// </summary>
        /// <returns> row/column dimension of this matrix
        /// </returns>
        public int Dimension
      {
         get
         {
            if (this.width != this.height)
            {
               throw new System.SystemException("Can't call getDimension() on a non-square matrix");
            }
            return this.width;
         }

      }

      // A helper to construct a square matrix.
      public BitMatrix(int dimension)
         : this(dimension, dimension)
      {
      }

      public BitMatrix(int width, int height)
      {
         if (width < 1 || height < 1)
         {
            throw new System.ArgumentException("Both dimensions must be greater than 0");
         }
         this.width = width;
         this.height = height;
         this.rowSize = (width + 31) >> 5;
            this.bits = new int[this.rowSize * height];
      }

      /// <summary> <p>Gets the requested bit, where true means black.</p>
      /// 
      /// </summary>
      /// <param name="x">The horizontal component (i.e. which column)
      /// </param>
      /// <param name="y">The vertical component (i.e. which row)
      /// </param>
      /// <returns> value of given bit in matrix
      /// </returns>
      public bool this[int x, int y]
      {
         get
         {
            int offset = y * this.rowSize + (x >> 5);
            return (((int)((uint)(this.bits[offset]) >> (x & 0x1f))) & 1) != 0;
         }
         set
         {
            if (value)
            {
               int offset = y * this.rowSize + (x >> 5);
                    this.bits[offset] |= 1 << (x & 0x1f);
            }
         }
      }

      /// <summary> <p>Flips the given bit.</p>
      /// 
      /// </summary>
      /// <param name="x">The horizontal component (i.e. which column)
      /// </param>
      /// <param name="y">The vertical component (i.e. which row)
      /// </param>
      public void flip(int x, int y)
      {
         int offset = y * this.rowSize + (x >> 5);
            this.bits[offset] ^= 1 << (x & 0x1f);
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

      /// <summary> <p>Sets a square region of the bit matrix to true.</p>
      /// 
      /// </summary>
      /// <param name="left">The horizontal position to begin at (inclusive)
      /// </param>
      /// <param name="top">The vertical position to begin at (inclusive)
      /// </param>
      /// <param name="width">The width of the region
      /// </param>
      /// <param name="height">The height of the region
      /// </param>
      public void setRegion(int left, int top, int width, int height)
      {
         if (top < 0 || left < 0)
         {
            throw new System.ArgumentException("Left and top must be nonnegative");
         }
         if (height < 1 || width < 1)
         {
            throw new System.ArgumentException("Height and width must be at least 1");
         }
         int right = left + width;
         int bottom = top + height;
         if (bottom > this.height || right > this.width)
         {
            throw new System.ArgumentException("The region must fit inside the matrix");
         }
         for (int y = top; y < bottom; y++)
         {
            int offset = y * this.rowSize;
            for (int x = left; x < right; x++)
            {
                    this.bits[offset + (x >> 5)] |= 1 << (x & 0x1f);
            }
         }
      }

      /// <summary> A fast method to retrieve one row of data from the matrix as a BitArray.
      /// 
      /// </summary>
      /// <param name="y">The row to retrieve
      /// </param>
      /// <param name="row">An optional caller-allocated BitArray, will be allocated if null or too small
      /// </param>
      /// <returns> The resulting BitArray - this reference should always be used even when passing
      /// your own row
      /// </returns>
      public BitArray getRow(int y, BitArray row)
      {
         if (row == null || row.Size < this.width)
         {
            row = new BitArray(this.width);
         }
         int offset = y * this.rowSize;
         for (int x = 0; x < this.rowSize; x++)
         {
            row.setBulk(x << 5, this.bits[offset + x]);
         }
         return row;
      }

      /// <summary>
      /// Sets the row.
      /// </summary>
      /// <param name="y">row to set</param>
      /// <param name="row">{@link BitArray} to copy from</param>
      public void setRow(int y, BitArray row)
      {
         Array.Copy(row.Array, 0, this.bits, y * this.rowSize, this.rowSize);
      }

      /// <summary>
      /// This is useful in detecting the enclosing rectangle of a 'pure' barcode.
      /// </summary>
      /// <returns>{left,top,width,height} enclosing rectangle of all 1 bits, or null if it is all white</returns>
      public int[] getEnclosingRectangle()
      {
         int left = this.width;
         int top = this.height;
         int right = -1;
         int bottom = -1;

         for (int y = 0; y < this.height; y++)
         {
            for (int x32 = 0; x32 < this.rowSize; x32++)
            {
               int theBits = this.bits[y * this.rowSize + x32];
               if (theBits != 0)
               {
                  if (y < top)
                  {
                     top = y;
                  }
                  if (y > bottom)
                  {
                     bottom = y;
                  }
                  if (x32 * 32 < left)
                  {
                     int bit = 0;
                     while ((theBits << (31 - bit)) == 0)
                     {
                        bit++;
                     }
                     if ((x32 * 32 + bit) < left)
                     {
                        left = x32 * 32 + bit;
                     }
                  }
                  if (x32 * 32 + 31 > right)
                  {
                     int bit = 31;
                     while (((int)((uint)theBits >> bit)) == 0) // (theBits >>> bit)
                     {
                        bit--;
                     }
                     if ((x32 * 32 + bit) > right)
                     {
                        right = x32 * 32 + bit;
                     }
                  }
               }
            }
         }

         int widthTmp = right - left;
         int heightTmp = bottom - top;

         if (widthTmp < 0 || heightTmp < 0)
         {
            return null;
         }

         return new [] { left, top, widthTmp, heightTmp };
      }

      /// <summary>
      /// This is useful in detecting a corner of a 'pure' barcode.
      /// </summary>
      /// <returns>{x,y} coordinate of top-left-most 1 bit, or null if it is all white</returns>
      public int[] getTopLeftOnBit()
      {
         int bitsOffset = 0;
         while (bitsOffset < this.bits.Length && this.bits[bitsOffset] == 0)
         {
            bitsOffset++;
         }
         if (bitsOffset == this.bits.Length)
         {
            return null;
         }
         int y = bitsOffset / this.rowSize;
         int x = (bitsOffset % this.rowSize) << 5;

         int theBits = this.bits[bitsOffset];
         int bit = 0;
         while ((theBits << (31 - bit)) == 0)
         {
            bit++;
         }
         x += bit;
         return new[] { x, y };
      }

      public int[] getBottomRightOnBit()
      {
         int bitsOffset = this.bits.Length - 1;
         while (bitsOffset >= 0 && this.bits[bitsOffset] == 0)
         {
            bitsOffset--;
         }
         if (bitsOffset < 0)
         {
            return null;
         }

         int y = bitsOffset / this.rowSize;
         int x = (bitsOffset % this.rowSize) << 5;

         int theBits = this.bits[bitsOffset];
         int bit = 31;

         while (((int)((uint)theBits >> bit)) == 0) // (theBits >>> bit)
         {
            bit--;
         }
         x += bit;

         return new int[] { x, y };
      }

      public override bool Equals(object obj)
      {
         if (!(obj is BitMatrix))
         {
            return false;
         }
         BitMatrix other = (BitMatrix)obj;
         if (this.width != other.width || this.height != other.height ||
             this.rowSize != other.rowSize || this.bits.Length != other.bits.Length)
         {
            return false;
         }
         for (int i = 0; i < this.bits.Length; i++)
         {
            if (this.bits[i] != other.bits[i])
            {
               return false;
            }
         }
         return true;
      }

      public override int GetHashCode()
      {
         int hash = this.width;
         hash = 31 * hash + this.width;
         hash = 31 * hash + this.height;
         hash = 31 * hash + this.rowSize;
         foreach (var bit in this.bits)
         {
            hash = 31 * hash + bit;
         }
         return hash;
      }

      public override string ToString()
      {
         var result = new System.Text.StringBuilder(this.height * (this.width + 1));
         for (int y = 0; y < this.height; y++)
         {
            for (int x = 0; x < this.width; x++)
            {
               result.Append(this[x, y] ? "X" : " ");
            }
            result.AppendLine("");
         }
         return result.ToString();
      }
   }
}