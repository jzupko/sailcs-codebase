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

using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using siat;
using jz.physics.narrowphase;

namespace jz.physics.broadphase
{
    public struct Pair : IComparable<Pair>
    {
        #region Public static members
        public static bool operator ==(Pair a, Pair b)
        {
            return (a.A == b.A) && (a.B == b.B);
        }

        // From: http://www.concentric.net/~Ttwang/tech/inthash.htm
        public static uint GetHash(ushort a, ushort b)
        {
            Order(ref a, ref b);

            uint c = (((uint)a) | (((uint)b) << 16));

            c = ~c + (c << 15);
            c = c ^ (c >> 12);
            c = c + (c << 2);
            c = c ^ (c >> 4);
            c = c * 2057;
            c = c ^ (c >> 16);

            return c;
        }

        public static bool operator !=(Pair a, Pair b)
        {
            return !(a == b);
        }
        
        public static void Order(ref ushort a, ref ushort b)
        {
            if (a > b) { Utilities.Swap(ref a, ref b); }
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            if (obj is Pair)
            {
                Pair b = (Pair)obj;
                return (this == b);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)GetHash();
        }
        #endregion

        public Pair(ushort a, ushort b)
        {
            A = a;
            B = b;

            Order(ref A, ref B);
        }

        public ushort A;
        public ushort B;

        public int CompareTo(Pair b)
        {
            int c = A.CompareTo(b.A);
            if (c == 0) { return B.CompareTo(b.B); }
            else { return c; }
        }

        public bool Equals(ushort a, ushort b)
        {
            Order(ref a, ref b);

            return (A == a) && (B == b);
        }
      
        public uint GetHash()
        {
            return GetHash(A, B);
        }
    }

    /// <summary>
    /// Called when a pair is added or removed.
    /// </summary>
    /// <param name="aPair">The added or removed Pair.</param>
    /// <remarks>
    /// Pair add/removes are batched and reported once per PairTable.Tick() call.
    /// </remarks>
    public delegate void PairCallback(ref Pair aPair);

    /// <summary>
    /// Called each tick for active pairs.
    /// </summary>
    /// <param name="aPair">The pair.</param>
    /// <param name="aPoints">The contact points of the pairs.</param>
    /// <remarks>
    /// This callback should be used to perform collision. Add/remove callbacks can be used
    /// for general preparation in response to the beginning or ending of pairs.
    /// </remarks>
    public delegate void PairPointCallback(ref Pair aPair, ref Arbiter aPoints);

    /// <summary>
    /// Handles management of pairs.
    /// </summary>
    /// <remarks>
    /// A pair in the physics system is two intersecting collision objects that require
    /// restitution.
    /// 
    /// Uses a combination hash-table with chaining and hash-table with open addressing idea.
    /// The array is effectively a double-linked list that uses array indices instead of pointers.
    /// Pairs are added contiguously into the array. When a pair is removed, the last pair in
    /// the array is placed in the hole and its "pointers" are updated to maintain contiguous
    /// ordering. Everthing is O(1) except for find (and consequently, remove) operations, which
    /// area O(n) in the worst case. This is dependent on the amount of collisions in the hash
    /// table which is usually dependent on the quality of the hashing function.
    /// 
    /// Based on ICE PairManager: http://www.codercorner.com/PairManager.rar .
    /// </remarks>
    public class PairTable
    {
        public const int kMinSizePower = (sizeof(byte) * 8);
        public const int kMaxSizePower = (sizeof(ushort) * 8);

        #region Private members
        private uint mMask;
        private ushort mNull;
        private int mSizePower;
        private uint mSize;

        private void _TickHelper(InsertionSortList<Pair> aList, PairCallback aCallback)
        {
            int count = aList.Count;
            for (int i = 0; i < count; i++)
            {
                aCallback(ref aList.Data[i]);
            }

            aList.Clear();
        }
        #endregion

        #region Protected members
        #region Types
        protected struct Node
        {
            public ushort Next;
            public ushort Prev;
        }
        #endregion

        #region Static members
        protected ushort _GetIndex(uint aHash)
        {
            ushort ret = (ushort)(aHash & mMask);

            return ret;
        }
        #endregion

        protected InsertionSortList<Pair> mAdds = new InsertionSortList<Pair>();
        protected InsertionSortList<Pair> mRemoves = new InsertionSortList<Pair>();
        protected uint mPairCount = 0u;

        // mList is effectively the prev/next pointers for the linked-list of Pair elements
        // in mPairs. It is stored as a separate array to allow the mPairs array to be returned
        // as a contiguous array of pairs.
        protected Node[] mList;
        protected Pair[] mPairs;
        protected Arbiter[] mPoints;
        protected ushort[] mTable;

        protected bool _Add(ushort a, ushort b)
        {
            uint hash = Pair.GetHash(a, b);
            ushort tableIndex = _GetIndex(hash);
            ushort pairIndex = mTable[tableIndex];

            while (pairIndex != mNull)
            {
                if (mPairs[pairIndex].Equals(a, b)) { return false; }

                pairIndex = mList[pairIndex].Next;
            }

            pairIndex = (ushort)mPairCount;

            if (mSize <= mPairCount)
            {
                if (mSizePower == kMaxSizePower) { return false; }
                else
                {
#if DEBUG
                    uint oldPairCount = mPairCount;
#endif
                    _Grow();
#if DEBUG
                    Debug.Assert(oldPairCount == mPairCount);
#endif
                }
            }

            tableIndex = _GetIndex(hash);
            ushort nextPairIndex = mTable[tableIndex];
            Debug.Assert(nextPairIndex == mNull || nextPairIndex < mPairCount);
            Debug.Assert(pairIndex != nextPairIndex);

            // Insert the new pair at the head of the list.
            mList[pairIndex].Next = nextPairIndex;
            if (nextPairIndex != mNull) { mList[nextPairIndex].Prev = pairIndex; }
            mList[pairIndex].Prev = mNull;

            // Update the head pointer in the table to the new pair.
            mTable[tableIndex] = pairIndex;

            // Add the new pair into the pair array.
            mPairs[pairIndex] = new Pair(a, b);
            mPoints[pairIndex].Contacts = new List<ContactPoint>(4);
            mPairCount++;

            return true;
        }

        protected void _Grow()
        {
            #region Pre-resize
            uint oldPairCount = mPairCount;
            Pair[] oldPairs = mPairs;
            #endregion

            #region Resize
            _SetSize(mSizePower + 1);
            mPairCount = 0u;
            Array.Resize(ref mList, (int)mSize);
            mPairs = new Pair[mSize];
            Array.Resize(ref mPoints, (int)mSize);
            Array.Resize(ref mTable, (int)mSize);
            #endregion

            #region Clear
            for (uint i = 0; i < mSize; i++) { mList[i].Next = mNull; mList[i].Prev = mNull; }
            for (uint i = 0; i < mSize; i++) { mTable[i] = mNull; }
            #endregion

            #region Reinsert old pairs.
            // This is necessary as growing the table changes the mask and changes the index
            // that a hash value maps to.
            for (uint i = 0; i < oldPairCount; i++)
            {
                _Add(oldPairs[i].A, oldPairs[i].B);
                Debug.Assert(oldPairs[i] == mPairs[i]);
            }
            #endregion
        }

        /// <summary>
        /// This ensures that the array is always contiguous.
        /// </summary>
        /// <param name="i">The index of the hole to patch.</param>
        protected void _PatchHole(ushort i)
        {
            Debug.Assert(mPairCount > 0u);
            uint lastIndex = (mPairCount - 1u);
            Debug.Assert(lastIndex >= i);

            if (lastIndex > i)
            {
                // Move the last node into the i position.
                mPairs[i] = mPairs[lastIndex];
                mPoints[i] = mPoints[lastIndex];
                mList[i] = mList[lastIndex];

                // Update the next and prev to the new position.
                if (mList[i].Next != mNull) { mList[mList[i].Next].Prev = i; }
                if (mList[i].Prev != mNull) { mList[mList[i].Prev].Next = i; }
                // if the index we used to patch is the head of its table entry, update the table entry.
                else
                {
                    uint hash = Pair.GetHash(mPairs[i].A, mPairs[i].B);
                    ushort tableIndex = _GetIndex(hash);

                    mTable[tableIndex] = i;
                }
            }

            mPairCount--;
        }

        protected bool _Remove(ushort a, ushort b)
        {
            uint hash = Pair.GetHash(a, b);
            ushort tableIndex = _GetIndex(hash);
            ushort i = mTable[tableIndex];

            #region If pair is the head of the table list.
            if (i != mNull)
            {
                if (mPairs[i].Equals(a, b))
                {
                    mTable[tableIndex] = mList[i].Next;
                    if (mList[i].Next != mNull) { mList[mList[i].Next].Prev = mNull; }

                    _PatchHole(i);

                    return true;
                }
            }
            else
            {
                return false;
            }
            #endregion

            #region Else
            for (i = mList[i].Next; i != mNull; i = mList[i].Next)
            {
                if (mPairs[i].Equals(a, b))
                {
                    if (mList[i].Next != mNull) { mList[mList[i].Next].Prev = mList[i].Prev; }
                    if (mList[i].Prev != mNull) { mList[mList[i].Prev].Next = mList[i].Next; }

                    _PatchHole(i);

                    return true;
                }
            }

            return false;
            #endregion
        }
      
        protected void _SetSize(int aSizePower)
        {
            mSizePower = aSizePower;
            // -1u to have a value for mNull - could cause overflow otherwise if the table reaches
            // the maximum size of a ushort.
            mSize = (uint)(1 << mSizePower) - 1u;
            mMask = mSize - 1u;
            mNull = (ushort)mSize;
        }
        #endregion

        public PairTable() : this(kMinSizePower) {}
        public PairTable(int aSizePower)
        {
            _SetSize(Utilities.Clamp(aSizePower - 1, kMinSizePower - 1, kMaxSizePower - 1));
            _Grow();
        }

        public void Add(ushort a, ushort b)
        {
            if (a != b)
            {
                if (_Add(a, b)) { mAdds.Add(new Pair(a, b)); }
            }
        }

        public bool Get(ushort a, ushort b, ref Pair arPair)
        {
            if (a == b)
            {
                return false;
            }

            uint hash = Pair.GetHash(a, b);
            uint tableIndex = _GetIndex(hash);

            if (mTable[tableIndex] == mNull)
            {
                return false;
            }
            else
            {
                for (uint i = mTable[tableIndex]; i != mNull; i = mList[i].Next)
                {
                    if (mPairs[i].Equals(a, b))
                    {
                        arPair = mPairs[i];
                        return true;
                    }
                }

                return false;
            }
        }

        public void GetAllPairs(ref Pair[] arPairs)
        {
            if (arPairs.Length != mPairCount) { Array.Resize(ref arPairs, (int)mPairCount); }
            Array.Copy(mPairs, 0, arPairs, 0, mPairCount);
        }

        public void Remove(ushort a)
        {
            for (uint i = 0; i < mPairCount; i++)
            {
                if (mPairs[i].A == a || mPairs[i].B == a)
                {
                    Remove(mPairs[i].A, mPairs[i].B);
                    i--;
                }
            }
        }

        public void Remove(ushort a, ushort b)
        {
            if (a != b)
            {
                if (_Remove(a, b))
                {
                    mRemoves.Add(new Pair(a, b));
                }
            }
        }

        public void Remove(BitArray aPairs)
        {
            for (uint i = 0; i < mPairCount; i++)
            {
                ushort a = mPairs[i].A;
                ushort b = mPairs[i].B;

                if (aPairs[a] || aPairs[b])
                {
                    Remove(a, b);

                    // Since we know the pair is in the list, it will always be removed,
                    // reducing pair count by 1 and placing a new pair in this index.
                    i--;
                }
            }
        }

        public void Tick(PairCallback aAddedCallback, PairPointCallback aUpdatedCallback, PairCallback aRemovedCallback)
        {
            _TickHelper(mAdds, aAddedCallback);
            _TickHelper(mRemoves, aRemovedCallback);

            for (uint i = 0; i < mPairCount; i++)
            {
                aUpdatedCallback(ref mPairs[i], ref mPoints[i]);
            }
        }
    }
}
