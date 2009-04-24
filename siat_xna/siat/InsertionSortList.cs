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

namespace siat
{
    /// <summary>
    /// A dynamic array like List<> that keeps its elements sorted using insertion sort.
    /// </summary>
    /// <remarks>
    /// Performance with this list will be best if the list is small and elements are inserted
    /// in roughly sorted order.
    /// </remarks>
    public sealed class InsertionSortList<T>
        where T : IComparable<T>
    {
        public const int kBinarySearchThreshold = 100;

        public const int kMinSize = (1 << 4);
        public const int kMaxSize = int.MaxValue;

        #region Private members
        T[] mData;
        int mDataCount = 0;
        int mLast = 0;

        private void _Grow()
        {
            int newSize = (mData.Length < (kMaxSize >> 1)) ? (mData.Length << 1) : kMaxSize;

            if (newSize == mData.Length)
            {
                throw new Exception("InsertionSortList exceeded maximum size.");
            }

            Array.Resize(ref mData, newSize);
        }
        #endregion

        public InsertionSortList() : this(kMinSize) { }
        public InsertionSortList(int aInitialSize)
        {
            mData = new T[Utilities.Clamp(aInitialSize, kMinSize, kMaxSize)];
        }

        public int Add(T a) { return Add(a, -1); }

        /// <summary>
        /// Adds an element sorted into the array.
        /// </summary>
        /// <param name="a">The element to add.</param>
        /// <returns>The index of insertion.</returns>
        /// <remarks>
        /// Uses insertion sort to maintain order, which is O(n) in the worse cast. 
        /// Performance can be better than alternatives with small lists that are built
        /// in roughly sorted order.
        /// </remarks>
        public int Add(T a, int aSearchStartIndex)
        {
            int index = IndexOf(a, aSearchStartIndex);

            // This element is not already in the array.
            if (index < 0)
            {
                index = ~index;

                if (mDataCount >= mData.Length) { _Grow(); }
                if (index < mDataCount) { Array.Copy(mData, index, mData, index + 1, (mDataCount - index)); }
                mData[index] = a;
                mDataCount++;
            }

            return index;
        }

        public void Clear()
        {
            Array.Clear(mData, 0, mDataCount);

            mDataCount = 0;
            mLast = 0;
        }

        public void Compact()
        {
            Array.Resize(ref mData, mDataCount);
        }

        public int Count { get { return mDataCount; } set { mDataCount = value; mLast = Utilities.Clamp(mLast, 0, mDataCount - 1); } }

        /// <summary>
        /// Returns the internal array.
        /// </summary>
        /// <remarks>
        /// A hack. If you muck with the array, maintain the sort order. 
        /// Used by jz.broadphase.Sap for more efficient update passes.
        /// </remarks>
        public T[] Data { get { return mData; } }

        public int IndexOf(T a) { return IndexOf(a, -1); }

        /// <summary>
        /// Finds the index of an element.
        /// </summary>
        /// <param name="a">The element to find.</param>
        /// <returns>The index of the element.</returns>
        /// <remarks>
        /// O(log N) or N. Uses binary search when mDataCount is above a threshold, otherwise a linear
        /// search, which should be faster below this threshold due to better cache coherency and simpler
        /// per step instructions. Can return a negative value with special meaning for insertion.
        /// See Array.BinarySearch() for more information.
        /// </remarks>
        public int IndexOf(T a, int aSearchStartIndex)
        {
            if (aSearchStartIndex >= 0) { mLast = aSearchStartIndex; }

            int compare;
            int ret = mLast;

            Utilities.Assert(mLast >= 0 && (mDataCount == 0 && mLast == 0 || mLast < mDataCount));

            // Binary search if mDataCount is above a certain threshold and we
            // weren't given an explicit starting point (aSearchStartIndex is < 0)
            if (aSearchStartIndex < 0 && mDataCount > kBinarySearchThreshold)
            {
                ret = Array.BinarySearch(mData, 0, mDataCount, a);
            }
            // Otherwise, do a linear search, starting at mLast.
            else
            {
                if (a.CompareTo(mData[mLast]) < 0)
                {
                    ret = ~0;

                    for (int i = mLast - 1; i >= 0; i--)
                    {
                        compare = a.CompareTo(mData[i]);

                        if (compare == 0) { ret = i; break; }
                        else if (compare > 0) { ret = ~(i + 1); break; }
                    }
                }
                else
                {
                    ret = ~mDataCount;

                    for (int i = mLast; i < mDataCount; i++)
                    {
                        compare = a.CompareTo(mData[i]);

                        if (compare == 0) { ret = i; break; }
                        else if (compare < 0) { ret = ~i; break; }
                    }
                }
            }

            // mLast is set to either the ret (if it's positive) or to the insertion point if it wasn't
            // (the complement of the negative number).
            if (ret >= 0) { mLast = ret; }

            return ret;
        }

        public int IndexOf(T a, int aSearchStartIndex, Comparer<T> aComparer)
        {
            if (aSearchStartIndex >= 0) { mLast = aSearchStartIndex; }

            int compare;
            int ret = mLast;

            Utilities.Assert(mLast >= 0 && (mDataCount == 0 && mLast == 0 || mLast < mDataCount));

            // Binary search if mDataCount is above a certain threshold and we
            // weren't given an explicit starting point (aSearchStartIndex is < 0)
            if (aSearchStartIndex < 0 && mDataCount > kBinarySearchThreshold)
            {
                ret = Array.BinarySearch(mData, 0, mDataCount, a, aComparer);
            }
            // Otherwise, do a linear search, starting at mLast.
            else
            {
                if (a.CompareTo(mData[mLast]) < 0)
                {
                    ret = ~0;

                    for (int i = mLast - 1; i >= 0; i--)
                    {
                        compare = aComparer.Compare(a, mData[i]);

                        if (compare == 0) { ret = i; break; }
                        else if (compare > 0) { ret = ~(i + 1); break; }
                    }
                }
                else
                {
                    ret = ~mDataCount;

                    for (int i = mLast; i < mDataCount; i++)
                    {
                        compare = aComparer.Compare(a, mData[i]);

                        if (compare == 0) { ret = i; break; }
                        else if (compare < 0) { ret = ~i; break; }
                    }
                }
            }

            // mLast is set to either the ret (if it's positive) or to the insertion point if it wasn't
            // (the complement of the negative number).
            if (ret >= 0) { mLast = ret; }

            return ret;
        }

        /// <summary>
        /// Removes an element at index i.
        /// </summary>
        /// <param name="i">The index of the element to remove.</param>
        /// <remarks>
        /// O(1) but potentially an expensive operation due to shift the array
        /// after the removed element.
        /// </remarks>
        public void Remove(int i)
        {
            if (i < 0 || i >= mDataCount) { return; }

            if (i < mDataCount - 1) { Array.Copy(mData, i + 1, mData, i, (mDataCount - (i + 1))); }
            mDataCount--;
            mLast = 0;
        }

        public int Remove(T a) { return Remove(a, -1); }

        /// <summary>
        /// Removes an element.
        /// </summary>
        /// <param name="a">The element to remove.</param>
        /// <param name="aSearchStartIndex">An index to start searching for the element to remove.</param>
        /// <returns>The index where the element was removed.</returns>
        /// <remarks>
        /// O(log N) to find the element to remove plus the time to shift the remainder of the array
        /// down one element.
        /// </remarks>
        public int Remove(T a, int aSearchStartIndex)
        {
            int index = IndexOf(a, aSearchStartIndex);

            Remove(index);

            return index;
        }
    }
}
