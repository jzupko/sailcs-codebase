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
using System.Runtime.InteropServices;
using jz.physics.narrowphase;
using siat;

namespace jz.physics.broadphase
{
    /// <summary>
    /// Sweep and prune, see: http://en.wikipedia.org/wiki/Sweep_and_prune .
    /// </summary>
    /// <remarks>
    /// Also: http://www.bulletphysics.com/Bullet/phpBB3/viewtopic.php?p=&f=&t=209
    ///       http://www.bulletphysics.com/Bullet/phpBB3/viewtopic.php?f=6&t=1497
    ///       http://www.bulletphysics.com/Bullet/phpBB3/viewtopic.php?f=6&t=1919
    /// 
    /// Based on Sweep-and-prune article and code by Pierre Terdiman: 
    ///     http://www.codercorner.com/SAP.pdf
    ///     http://www.codercorner.com/Code/SweepAndPrune.rar
    /// </remarks>
    public class Sap : IBroadphase
    {
        public static readonly Vector3 kCollisionBoundary = new Vector3(PhysicsConstants.kMinimumThickness);

        public const ushort kSentinelId = ushort.MaxValue;
        public const uint kSentinelMin = uint.MinValue;
        public const uint kSentinelMax = uint.MaxValue;

        #region Protected members
        protected List<Arbiter> mArbiters;

        protected PairCallback mAddHandler;
        protected PairCallback mRemoveHandler;
        protected PairPointCallback mUpdateHandler;
        
        protected void _AddHandler(ref Pair aPair)
        {
            Body a = mAABBs.Data[aPair.A].Object;
            Body b = mAABBs.Data[aPair.B].Object;
        }
        
        protected void _RemoveHandler(ref Pair aPair)
        {
            Body a = mAABBs.Data[aPair.A].Object;
            Body b = mAABBs.Data[aPair.B].Object;
        }

        protected void _UpdateHandler(ref Pair aPair, ref Arbiter aPoints)
        {
            Body a = mAABBs.Data[aPair.A].Object;
            Body b = mAABBs.Data[aPair.B].Object;

            aPoints.A = a;
            aPoints.B = b;
            mArbiters.Add(aPoints);
        }

        protected static bool Collideable(ref AABBEntry entry, ref AABBEntry jEntry)
        {
            bool bReturn = (entry.Object.CollidesWith & jEntry.Object.Type) != 0 &&
                           (entry.Object.Type & jEntry.Object.CollidesWith) != 0;

            return bReturn;
        }

        protected static bool Intersects(ref AABBEntry aMin, ref AABBEntry bMax, int axis0)
        {
            unsafe
            {
                fixed (int* paMin = aMin.Min, pbMax = bMax.Max)
                {
                    bool bReturn = !(pbMax[axis0] < paMin[axis0]);

                    return bReturn;
                }
            }
        }

        protected static bool Intersects(ref AABBEntry a, ref AABBEntry b, int axis1, int axis2)
        {
            unsafe
            {
                fixed (int* paMin = a.Min, paMax = a.Max, pbMin = b.Min, pbMax = b.Max)
                {
                    bool bReturn = !((pbMax[axis1] < paMin[axis1]) ||
                                     (paMax[axis1] < pbMin[axis1]) ||
                                     (pbMax[axis2] < paMin[axis2]) ||
                                     (paMax[axis2] < pbMin[axis2]));

                    return bReturn;
                } 
            }
        }

        AddressList<AABBEntry> mAABBs = new AddressList<AABBEntry>();

        SimpleArray<EndPoint>[] mAxes = new SimpleArray<EndPoint>[]
            { new SimpleArray<EndPoint>(),
              new SimpleArray<EndPoint>(),
              new SimpleArray<EndPoint>() };

        List<ushort> mRemoves = new List<ushort>();

        PairTable mPairs;
        BitArray mPairRemoveCache = new BitArray(ushort.MaxValue);

        protected void _MoveMaxLeft(int aAxisIndex, ref EndPoint n, int startIndex)
        {
            int axis1 = (1 << aAxisIndex) & 3;
            int axis2 = (1 << axis1) & 3;

            ushort nhandle = n.OwnerId;
            EndPoint[] data = mAxes[aAxisIndex].Data;

            int j = startIndex - 1;
            for (; n.Value < data[j].Value; j--)
            {
                ushort jhandle = data[j].OwnerId;
                data[j + 1] = data[j];

                if (!data[j + 1].IsMax)
                {
                    mAABBs.Data[jhandle].AdjustMin(aAxisIndex, 1);

                    if (Intersects(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle], axis1, axis2))
                    {
                        mPairs.Remove(nhandle, jhandle);
                    }
                }
                else
                {
                    mAABBs.Data[jhandle].AdjustMax(aAxisIndex, 1);
                }
            }

            data[j + 1] = n;
            mAABBs.Data[nhandle].SetMax(aAxisIndex, j + 1);
        }

        protected void _MoveMaxRight(int aAxisIndex, ref EndPoint n, int startIndex)
        {
            int axis1 = (1 << aAxisIndex) & 3;
            int axis2 = (1 << axis1) & 3;

            ushort nhandle = n.OwnerId;
            EndPoint[] data = mAxes[aAxisIndex].Data;

            int j = startIndex + 1;
            for (; n.Value > data[j].Value; j++)
            {
                ushort jhandle = data[j].OwnerId;
                data[j - 1] = data[j];

                if (!data[j - 1].IsMax)
                {
                    mAABBs.Data[jhandle].AdjustMin(aAxisIndex, -1);

                    if (Collideable(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle])
                        && Intersects(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle], aAxisIndex)
                        && Intersects(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle], axis1, axis2))
                    {
                        mPairs.Add(nhandle, jhandle);
                    }
                }
                else
                {
                    mAABBs.Data[jhandle].AdjustMax(aAxisIndex, -1);
                }
            }

            data[j - 1] = n;
            mAABBs.Data[nhandle].SetMax(aAxisIndex, j - 1);
        }

        protected void _MoveMinLeft(int aAxisIndex, ref EndPoint n, int startIndex)
        {
            int axis1 = (1 << aAxisIndex) & 3;
            int axis2 = (1 << axis1) & 3;

            ushort nhandle = n.OwnerId;
            EndPoint[] data = mAxes[aAxisIndex].Data;

            int j = startIndex - 1;
            for (; n.Value < data[j].Value; j--)
            {
                ushort jhandle = data[j].OwnerId;
                data[j + 1] = data[j];

                if (data[j + 1].IsMax)
                {
                    mAABBs.Data[jhandle].AdjustMax(aAxisIndex, 1);

                    if (Collideable(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle])
                        // Note: order of arguments to this function is important. The first argument is
                        //       a min and the second a max. We want the min of the "other" endpoint
                        //       and the max of the endpoint being moved in this case since we're
                        //       moving the min.
                        && Intersects(ref mAABBs.Data[jhandle], ref mAABBs.Data[nhandle], aAxisIndex)
                        && Intersects(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle], axis1, axis2))
                    {
                        mPairs.Add(nhandle, jhandle);
                    }
                }
                else
                {
                    mAABBs.Data[jhandle].AdjustMin(aAxisIndex, 1);
                }
            }

            data[j + 1] = n;
            mAABBs.Data[nhandle].SetMin(aAxisIndex, j + 1);
        }

        protected void _MoveMinRight(int aAxisIndex, ref EndPoint n, int startIndex)
        {
            int axis1 = (1 << aAxisIndex) & 3;
            int axis2 = (1 << axis1) & 3;

            ushort nhandle = n.OwnerId;
            EndPoint[] data = mAxes[aAxisIndex].Data;

            int j = startIndex + 1;
            for (; n.Value > data[j].Value; j++)
            {
                ushort jhandle = data[j].OwnerId;
                data[j - 1] = data[j];

                if (data[j - 1].IsMax)
                {
                    mAABBs.Data[jhandle].AdjustMax(aAxisIndex, -1);

                    if (Intersects(ref mAABBs.Data[nhandle], ref mAABBs.Data[jhandle], axis1, axis2))
                    {
                        mPairs.Remove(nhandle, jhandle);
                    }
                }
                else
                {
                    mAABBs.Data[jhandle].AdjustMin(aAxisIndex, -1);
                }
            }

            data[j - 1] = n;
            mAABBs.Data[nhandle].SetMin(aAxisIndex, j - 1);
        }

        protected void _RemoveHelper(List<int> aToRemoves, int aAxisIndex)
        {
            if (aToRemoves.Count > 0)
            {
                aToRemoves.Sort();
                SimpleArray<EndPoint> array = mAxes[aAxisIndex];
                EndPoint[] a = mAxes[aAxisIndex].Data;
                int count = aToRemoves.Count;

                for (int i = 0; i < count - 1; i++)
                {
                    int offset = (i + 1);
                    int start = aToRemoves[i];
                    int end = aToRemoves[i + 1] - offset;

                    // 0 and -1 to avoid touching sentinels.
                    Debug.Assert(start > 0 && start < array.Count - 1);
                    Debug.Assert(end > 0 && end < array.Count - 1);

                    for (int j = start; j <= end; j++)
                    {
                        a[j] = a[j + offset];
                        if (a[j].IsMax) { mAABBs.Data[a[j].OwnerId].AdjustMax(aAxisIndex, -offset); }
                        else { mAABBs.Data[a[j].OwnerId].AdjustMin(aAxisIndex, -offset); }
                    }
                }

                int newAxisCount = (array.Count - count);

                for (int i = (aToRemoves[count - 1] - count + 1); i < newAxisCount - 1; i++)
                {
                    a[i] = a[i + count];
                    if (a[i].IsMax) { mAABBs.Data[a[i].OwnerId].AdjustMax(aAxisIndex, -count); }
                    else { mAABBs.Data[a[i].OwnerId].AdjustMin(aAxisIndex, -count); }
                }

                // move sentinel.
                a[newAxisCount - 1] = a[array.Count - 1];

                array.Count = newAxisCount;
                aToRemoves.Clear();
            }
        }

        protected void _TickRemovesA()
        {
            if (mRemoves.Count > 0)
            {
                mPairs.Remove(mPairRemoveCache);
            }
        }

        protected List<int> mRemovesX = new List<int>();
        protected List<int> mRemovesY = new List<int>();
        protected List<int> mRemovesZ = new List<int>();

        protected void _TickRemovesB()
        {
            int count = mRemoves.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    ushort handle = mRemoves[i];
                    mPairRemoveCache[handle] = false;

                    mRemovesX.Add(mAABBs.Data[handle].MinX);
                    mRemovesX.Add(mAABBs.Data[handle].MaxX);
                    mRemovesY.Add(mAABBs.Data[handle].MinY);
                    mRemovesY.Add(mAABBs.Data[handle].MaxY);
                    mRemovesZ.Add(mAABBs.Data[handle].MinZ);
                    mRemovesZ.Add(mAABBs.Data[handle].MaxZ);
                }

                _RemoveHelper(mRemovesX, 0);
                _RemoveHelper(mRemovesY, 1);
                _RemoveHelper(mRemovesZ, 2);

                for (int i = 0; i < count; i++)
                {
                    ushort handle = mRemoves[i];
                    mAABBs.Data[handle].Object = null;
                    mAABBs.Remove(handle);
                }
                mRemoves.Clear();
            }
        }

        protected void _UpdateHelper(ushort aHandle, int aAxisIndex, int aOldIndex, uint aNewValue)
        {
            int axisCount = mAxes[aAxisIndex].Count;
            EndPoint[] data = mAxes[aAxisIndex].Data;

            uint oldValue = data[aOldIndex].Value;

            // Move to the right.
            if (aNewValue > oldValue)
            {
                bool bMax = data[aOldIndex].IsMax;
                EndPoint n = new EndPoint(bMax, aHandle, aNewValue);

                if (bMax) { _MoveMaxRight(aAxisIndex, ref n, aOldIndex); }
                else { _MoveMinRight(aAxisIndex, ref n, aOldIndex); }
            }
            // Move to the left.
            else if (aNewValue < oldValue)
            {
                bool bMax = data[aOldIndex].IsMax;
                EndPoint n = new EndPoint(bMax, aHandle, aNewValue);

                if (bMax) { _MoveMaxLeft(aAxisIndex, ref n, aOldIndex); }
                else { _MoveMinLeft(aAxisIndex, ref n, aOldIndex); }
            }
            // else, do nothing - value hasn't changed
        }

        #region Types
        [StructLayout(LayoutKind.Explicit)]
        protected struct AABBEntry
        {
            public AABBEntry(Body aObject)
            {
                MaxX = -1;
                MaxY = -1;
                MaxZ = -1;

                MinX = -1;
                MinY = -1;
                MinZ = -1;

                Object = aObject;
            }

            // Intentional - C#'s version of a union.
            [FieldOffset(0)]
            public unsafe fixed int Min[3];
            [FieldOffset(0)]
            public int MinX;
            [FieldOffset(4)]
            public int MinY;
            [FieldOffset(8)]
            public int MinZ;

            // Intentional - C#'s version of a union.
            [FieldOffset(12)]
            public unsafe fixed int Max[3];
            [FieldOffset(12)]
            public int MaxX;
            [FieldOffset(16)]
            public int MaxY;
            [FieldOffset(20)]
            public int MaxZ;

            [FieldOffset(24)]
            public Body Object;

            public uint GetMaxValue(int aAxisIndex, EndPoint[] a)
            {
                unsafe
                {
                    fixed (int* p = Max)
                    {
                        return a[p[aAxisIndex]].Value;
                    }
                }
            }

            public uint GetMinValue(int aAxisIndex, EndPoint[] a)
            {
                unsafe
                {
                    fixed (int* p = Min)
                    {
                        return a[p[aAxisIndex]].Value;
                    }
                }
            }

            public void AdjustMax(int aAxisIndex, int aValue)
            {
                unsafe
                {
                    fixed (int* p = Max)
                    {
                        p[aAxisIndex] += aValue;
                    }
                }
            }

            public void AdjustMin(int aAxisIndex, int aValue)
            {
                unsafe
                {
                    fixed (int* p = Min)
                    {
                        p[aAxisIndex] += aValue;
                    }
                }
            }

            public void SetMax(int aAxisIndex, int aValue)
            {
                unsafe
                {
                    fixed (int* p = Max)
                    {
                        p[aAxisIndex] = aValue;
                    }
                }
            }

            public void SetMin(int aAxisIndex, int aValue)
            {
                unsafe
                {
                    fixed (int* p = Min)
                    {
                        p[aAxisIndex] = aValue;
                    }
                }
            }
        }

        protected struct EndPoint : IComparable<EndPoint>
        {
            public const uint kMinMaxFlag = (1 << 0);

            public const int kOwnerShift = (int)kMinMaxFlag;
            public const uint kOwnerMask = uint.MaxValue & (~kMinMaxFlag);

            #region Private members
            // bit 0:     flag indicating min/max
            // bit 1:     flag indicating no sentinel/sentinel.
            // bits 2-31: index of AABB
            private uint mOwnerAndFlags;

            #endregion

            public EndPoint(bool abMax, ushort aOwnerHandle, uint aValue)
            {
                mOwnerAndFlags = 0u;
                Value = aValue;

                IsMax = abMax;
                OwnerId = aOwnerHandle;
            }

            public int CompareTo(EndPoint b)
            {
                return Value.CompareTo(b.Value);
            }

            public bool IsMax
            {
                get
                {
                    return ((mOwnerAndFlags & kMinMaxFlag) != 0);
                }

                set
                {
                    if (value) { mOwnerAndFlags |= kMinMaxFlag; }
                    else { mOwnerAndFlags &= ~kMinMaxFlag; }
                }
            }

            public bool IsSentinel
            {
                get
                {
                    return (OwnerId == kSentinelId);
                }
            }

            public ushort OwnerId
            {
                get
                {
                    return (ushort)((mOwnerAndFlags & kOwnerMask) >> kOwnerShift);
                }

                set
                {
                    mOwnerAndFlags |= (uint)((((uint)value) << kOwnerShift) & kOwnerMask);
                }
            }

            public uint Value;
        }
        #endregion
        #endregion

        #region Overrides
        /// <summary>
        /// Adds a new object to the sap.
        /// </summary>
        /// <param name="aCollideable">The object to add.</param>
        /// <param name="aAABB">The initial AABB of the object.</param>
        /// <returns>A handle to the object used for future calls to Sap.Update() and Sap.Remove()</returns>
        public ushort Add(Body aCollideable, ref BoundingBox aAABB)
        {
            Debug.Assert(aCollideable != null);

            BoundingBox aabb = aAABB;
            aabb.Max += kCollisionBoundary;
            aabb.Min -= kCollisionBoundary;

            Utilities.Clamp(ref aabb, ref PhysicsConstants.kMaximumAABB, out aabb);

            ushort handle = (ushort)mAABBs.Add(new AABBEntry(aCollideable));
            AABBEntry[] data = mAABBs.Data;

            // I don't like this. Need a more graceful way of handling this.
            if (handle == kSentinelId) { throw new Exception("Exceeded maximum number of physical objects."); }

            for (int i = 0; i < 3; i++)
            {
                int min = mAxes[i].Count - 1;
                int max = mAxes[i].Count;
                mAxes[i].Data[min] = new EndPoint(false, handle, kSentinelMax - 2);
                mAxes[i].Add(new EndPoint(true, handle, kSentinelMax - 1));
                mAxes[i].Add(new EndPoint(true, kSentinelId, kSentinelMax));

                data[handle].SetMin(i, min);
                data[handle].SetMax(i, max);
            }

            Update(handle, ref aabb);

            return handle;
        }

        public ushort CurrentMaxHandle
        {
            get
            {
                return (ushort)(mAABBs.Count - 1u);
            }
        }

        /// <summary>
        /// Removes an object with the handle aHandle.
        /// </summary>
        /// <param name="aHandle">The handle of the object to remove.</param>
        /// <remarks>
        /// Handles are invalid after this call. Reusing the handle after a call to 
        /// remove will result in undefined behavior.
        /// </remarks>
        public void Remove(ushort aHandle)
        {
            Debug.Assert(aHandle != kSentinelId);

            if (!mPairRemoveCache[aHandle])
            {
                mPairRemoveCache[aHandle] = true;
                mRemoves.Add(aHandle);
            }
        }

        public void Tick(List<Arbiter> aArbiters)
        {
            mArbiters = aArbiters;

            // Remove the axis entries, then the pairs (and dispatch callbacks, which need the object entries), 
            // then remove the object entires.
            _TickRemovesA();
            mPairs.Tick(mAddHandler, mUpdateHandler, mRemoveHandler);
            _TickRemovesB();
        }

        /// <summary>
        /// Batches an object with handle aHandle for update to the new AABB aNewAABB.
        /// </summary>
        /// <param name="aHandle">The handle of the object to update.</param>
        /// <param name="aNewAABB">The new AABB.</param>
        public void Update(ushort aHandle, ref BoundingBox aNewAABB)
        {
            Debug.Assert(aHandle != kSentinelId);
            AABBEntry[] data = mAABBs.Data;

            BoundingBox aabb = aNewAABB;
            aabb.Max += kCollisionBoundary;
            aabb.Min -= kCollisionBoundary;

            Utilities.Clamp(ref aabb, ref PhysicsConstants.kMaximumAABB, out aabb);

            // If in the pair remove cache, this entry is going to be removed anyway so don't update.
            if (!mPairRemoveCache[aHandle])
            {
                _UpdateHelper(aHandle, (int)Axis.X, data[aHandle].MinX, Utilities.GetSortableUintFromFloat(aabb.Min.X));
                _UpdateHelper(aHandle, (int)Axis.X, data[aHandle].MaxX, Utilities.GetSortableUintFromFloat(aabb.Max.X));
                _UpdateHelper(aHandle, (int)Axis.Y, data[aHandle].MinY, Utilities.GetSortableUintFromFloat(aabb.Min.Y));
                _UpdateHelper(aHandle, (int)Axis.Y, data[aHandle].MaxY, Utilities.GetSortableUintFromFloat(aabb.Max.Y));
                _UpdateHelper(aHandle, (int)Axis.Z, data[aHandle].MinZ, Utilities.GetSortableUintFromFloat(aabb.Min.Z));
                _UpdateHelper(aHandle, (int)Axis.Z, data[aHandle].MaxZ, Utilities.GetSortableUintFromFloat(aabb.Max.Z));
            }
        }
        #endregion

        public Sap() : this(PairTable.kMinSizePower) { }
        public Sap(int aPairTableSizePower)
        {
            mAddHandler = _AddHandler;
            mRemoveHandler = _RemoveHandler;
            mUpdateHandler = _UpdateHandler;

            mPairs = new PairTable(aPairTableSizePower);

            for (int i = 0; i < 3; i++)
            {
                // Order of adds is important to maintain ascending order.
                mAxes[i].Add(new EndPoint(false, kSentinelId, kSentinelMin));
                mAxes[i].Add(new EndPoint(true, kSentinelId, kSentinelMax));
            }
        }

    }
}
