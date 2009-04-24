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

namespace siat.render
{
    public delegate void RenderNodeDelegate(RenderNode aNode, object aInstance);

    /// <summary>
    /// Basic unit of the render tree, which is traversed each frame to draw content.
    /// </summary>
    /// <remarks>
    /// RenderNodes are nodes in a tree. Each represents a render operation through a delegate
    /// function (usually a function in RenderOperations, but any function that fits the expected
    /// prototype). RenderNodes are instantiated through the .Adopt*() functions. These functions take
    /// a delegate function and an object instance, which is some arbitrary data the function operates on.
    /// Each Adopt function has different semantics, see these for more information. Adopt returns a RenderNode
    /// that is a child of the RenderNode that the Adopt function was called on.
    /// 
    /// The purpose of RenderNodes is to avoid duplicate render operations when possible. For example, if
    /// two meshes use the same Effect, the instance parameter for the render operation that sets the
    /// Effect will be equal. As a result, the Adopt() function will return an existing RenderNode instead
    /// of a new RenderNode. At render time, the Effect will be set once, and then all operations which
    /// depend on that Effect will occur. This avoids potentially setting, unsetting, and reseting the Effect
    /// multiple times per frame.
    /// 
    /// \warning A RenderNode is built for performance and has many specific semantics. Be very careful
    ///          if you plan to use a RenderNode outside of these semantics. In particular, do not store
    ///          a RenderNode. RenderNode is built around the ResetPool, which expects all nodes to be 
    ///          invalidated each frame. Storing a RenderNode or using new() to create RenderNodes will
    ///          result in unusual, probably undesired behavior.
    /// 
    /// \todo A RenderNode is very heavy. A large number of renderables can result in 10s of MBs of
    ///       memory used just for RenderNodes each frame. This design was a result of several iterations to
    ///       balance performance with flexibility and extendability of the render tree. A better design
    ///       may be possible that reduces memory usage but for now, this is an acceptable compromise.
    /// 
    /// \todo The RenderNode scheme is probably not the final scheme for organizing render operations 
    ///       It requires O(n) time for sorting each frame in the worst case and it may also be unnecessarily
    ///       heavy and overly flexible for what is needed. It is however convenient for the time being while
    ///       tweaking and testing different rendering orders.
    /// </remarks>
    public sealed class RenderNode
    {   
        #region Private members
        internal RenderNode mHead = null;
        private RenderNode mNext = null;

        private RenderNodeDelegate mDelegate = null;
        private object mInstance = null;
        private float mSortOrder = float.MaxValue;

        private static class RenderPool
        {
            public static RenderNode Grab(RenderNodeDelegate aDelegate, object aInstance)
            {
                RenderNode node = ResetPool<RenderNode>.Grab();
                node.mDelegate = aDelegate;
                node.mInstance = aInstance;
                node._Reset();

                return node;
            }

            public static RenderNode Grab(RenderNodeDelegate aDelegate, object aInstance, float aSortOrder)
            {
                RenderNode node = Grab(aDelegate, aInstance);
                node.mSortOrder = aSortOrder;

                return node;
            }

            public static void Reset()
            {
                ResetPool<RenderNode>.Reset();
            }
        }

        private void _Reset()
        {
            mHead = null;
            mNext = null;
            mSortOrder = float.MaxValue;
        }

        private void _ReinsertSorted(RenderNode node)
        {
            if (mHead == null || mHead.mSortOrder > node.mSortOrder)
            {
                node.mNext = mHead;
                mHead = node;
            }
            else
            {
                RenderNode prev = mHead;
                RenderNode e = mHead.mNext;
                for (; e != null; e = e.mNext)
                {
                    if (e.mSortOrder > node.mSortOrder) { break; }
                    prev = e;
                }

                prev.mNext = node;
                node.mNext = e;
            }
        }

        public RenderNode() 
        { }
        #endregion

        public RenderNode Adopt(RenderNodeDelegate aDelegate, object aInstance)
        {
            for (RenderNode e = mHead; e != null; e = e.mNext)
            {
                if (e.mInstance.Equals(aInstance)) { return e; }
            }

            return AdoptFront(aDelegate, aInstance);
        }

        public RenderNode AdoptAndUpdateSort(RenderNodeDelegate aDelegate, object aInstance, float aSortOrder)
        {
            RenderNode prev = null;

            for (RenderNode e = mHead; e != null; e = e.mNext)
            {
                if (e.mInstance.Equals(aInstance))
                {
                    e.mSortOrder = Utilities.Min(e.mSortOrder, aSortOrder);
                    if (prev != null) { prev.mNext = e.mNext; }
                    else { mHead = e.mNext; }

                    _ReinsertSorted(e);

                    return e;
                }

                prev = e;
            }

            return AdoptSorted(aDelegate, aInstance, aSortOrder);
        }

        public RenderNode AdoptFront(RenderNodeDelegate aDelegate, object aInstance)
        {
            RenderNode ret = RenderPool.Grab(aDelegate, aInstance);
            ret.mNext = mHead;
            mHead = ret;

            return ret;
        }

        public RenderNode AdoptSorted(RenderNodeDelegate aDelegate, object aInstance, float aSortOrder)
        {
            if (mHead == null || mHead.mSortOrder > aSortOrder)
            {
                RenderNode ret = AdoptFront(aDelegate, aInstance);
                ret.mSortOrder = aSortOrder;

                return ret;
            }
            else if (mHead != null && mHead.mSortOrder == aSortOrder && mHead.mInstance == aInstance)
            {
                return mHead;
            }
            else
            {
                RenderNode prev = mHead;
                RenderNode e = mHead.mNext;
                for (; e != null; e = e.mNext)
                {
                    if (e.mSortOrder == aSortOrder && e.mInstance.Equals(aInstance)) { return e; }
                    else if (e.mSortOrder > aSortOrder) { break; }

                    prev = e;
                }

                RenderNode ret = RenderPool.Grab(aDelegate, aInstance, aSortOrder);
                prev.mNext = ret;
                ret.mNext = e;

                return ret;
            }
        }

        public void Render() { mDelegate(this, mInstance); }
        public static RenderNode SpawnRoot() { return new RenderNode(); }

        public void RenderChildren()
        {
            for (RenderNode e = mHead; e != null; e = e.mNext) { e.Render(); }
        }

        public void RenderChildrenAndReset()
        {
            RenderChildren();
            RenderPool.Reset();
            _Reset();
        }

        public void Reset()
        {
            RenderPool.Reset();
            _Reset();
        }
    }
}
