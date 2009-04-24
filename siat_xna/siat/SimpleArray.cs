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
    public sealed class SimpleArray<T>
    {
        public const int kMinSize = (1 << 2);
        public const int kMaxSize = int.MaxValue;

        #region Private members
        T[] mData;
        int mDataCount = 0;

        private void _Grow()
        {
            int newSize = (mData.Length < (kMaxSize >> 1)) ? (mData.Length << 1) : kMaxSize;

            Array.Resize(ref mData, newSize);
        }
        #endregion

        public SimpleArray() : this(kMinSize) { }
        public SimpleArray(int aInitialSize)
        {
            mData = new T[Utilities.Clamp(aInitialSize, kMinSize, kMaxSize)];
        }

        public void Add(T a)
        {
            if (mDataCount == mData.Length)
            {
                if (mData.Length < kMaxSize)
                {
                    _Grow();
                }
                else
                {
                    throw new Exception("CompactList exceeded maximum size.");
                }
            }

            mData[mDataCount++] = a;
        }

        public void Clear()
        {
            Array.Clear(mData, 0, mDataCount);
            mDataCount = 0;
        }

        public int Count
        {
            get
            {
                return mDataCount;
            }
            
            set
            {
                mDataCount = Utilities.Clamp(value, 0, kMaxSize);

                while (mData.Length < mDataCount)
                {
                    _Grow();
                }
            }
        }

        public T[] Data { get { return mData; } }
    }
}
