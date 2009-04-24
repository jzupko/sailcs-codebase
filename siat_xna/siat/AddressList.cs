//
// Copyright (c) 2009 Joseph A. Zupko
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Text;

namespace siat
{
    /// <summary>
    /// A dynamic array container like List<> that uses a free list to fill in holes
    /// when adding.
    /// </summary>
    /// <remarks>
    /// Because AddressList does not resize its array on remove, indices can effectively
    /// be treated as handles for the lifetime of the object in the array, since the index
    /// of an inserted object will not change while it is in the list.
    /// </remarks>
    public sealed class AddressList<T>
    {
        public const int kMinSize = (1 << 4);
        public const int kMaxSize = int.MaxValue;

        #region Private members
        T[] mData;
        int[] mFreeList;

        int mDataCount = 0;
        int mFreeCount = 0;

        private void _Grow()
        {
            int newSize = (mData.Length < (kMaxSize >> 1)) ? (mData.Length << 1) : kMaxSize;

            Array.Resize(ref mData, newSize);
            Array.Resize(ref mFreeList, (newSize >> 1));
        }
        #endregion

        public AddressList() : this(kMinSize) { }
        public AddressList(int aInitialSize)
        {
            mData = new T[Utilities.Clamp(aInitialSize, kMinSize, kMaxSize)];
            mFreeList = new int[(mData.Length >> 1)];
        }

        /// <summary>
        /// Adds an object T to the list.
        /// </summary>
        /// <param name="a">The object to add.</param>
        /// <returns>The index where the object was added. Can effectively be used as a handle.</returns>
        public int Add(T a)
        {
            if (mDataCount == mData.Length)
            {
                if (mData.Length < kMaxSize)
                {
                    _Grow();
                }
                else
                {
                    throw new Exception("AddressList exceeded maximum size.");
                }
            }

            int index = (mFreeCount > 0) ? mFreeList[--mFreeCount] : mDataCount++;
            mData[index] = a;

            return index;
        }

        public void Clear()
        {
            Array.Clear(mData, 0, mDataCount);
            Array.Clear(mFreeList, 0, mFreeCount);
            mDataCount = 0;
            mFreeCount = 0;
        }

        public int Count { get { return mDataCount; } }
        public T[] Data { get { return mData; } }

        public void Remove(int aHandle)
        {
            if (aHandle >= mDataCount) { return; }
            if (mFreeList.Length == mFreeCount)
            {
                if (mData.Length < kMaxSize)
                {
                    _Grow();
                }
                else
                {
                    throw new Exception("AddressList exceeded maximum size.");
                }
            }

            mFreeList[mFreeCount++] = aHandle;
            mData[aHandle] = default(T);
        }
    }
}
