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

using System.Collections.Generic;

namespace siat
{
    /// <summary>
    /// Base class for a doubly-linked tree with N children.
    /// </summary>
    public abstract class TreeNode<T> where T : TreeNode<T>
    {
        #region Private members
        private void _AdoptChild(T aChild)
        {
            if (aChild.mParent != null)
            {
                aChild._LeaveParent();
            }

            aChild.mParent = (T)this;
            aChild.mPrevSibling = mLastChild;

            if (mLastChild != null)
            {
                mLastChild.mNextSibling = aChild;
            }

            mLastChild = aChild;

            if (mFirstChild == null)
            {
                mFirstChild = aChild;
            }
#if DEBUG
            mChildCount++;
#endif
        }

        private void _LeaveParent()
        {
            if (mNextSibling != null)
            {
                mNextSibling.mPrevSibling = mPrevSibling;
            }

            if (mParent != null && mNextSibling == null)
            {
                mParent.mLastChild = mPrevSibling;
            }

            if (mPrevSibling != null)
            {
                mPrevSibling.mNextSibling = mNextSibling;
            }
            else if (mParent != null)
            {
                mParent.mFirstChild = mNextSibling;
            }

#if DEBUG
            if (mParent != null)
            {
                mParent.mChildCount--;
            }
#endif
            mNextSibling = null;
            mParent = null;
            mPrevSibling = null;
        }
        #endregion

        #region Protected members
        protected T mFirstChild = null;
        protected T mLastChild = null;
        protected T mNextSibling = null;
        protected T mParent = null;
        protected T mPrevSibling = null;

#if DEBUG
        protected int mChildCount = 0;
#endif

        protected TreeNode()
        { }
        #endregion

        #region Types for Apply()
        /// <summary>
        /// Determines recursive behavior of Apply.
        /// </summary>
        /// <remarks>
        /// - RecurseDown starts at this node and recurses to children.
        /// - RecurseUp starts at this node and recurses to parents.
        /// - Single applies only to this node.
        /// </remarks>
        public enum ApplyType
        {
            RecurseDown,
            RecurseUp,
            Single
        }

        /// <summary>
        /// Determines stopping behavior of Apply.
        /// </summary>
        /// <remarks>
        /// - First stops at the first node of type U.
        /// - Delegate stops when the delegate d() returns true.
        /// </remarks>
        public enum ApplyStop
        {
            First,
            Delegate
        }

        public delegate bool ApplyFunction<U>(U aNode) where U : TreeNode<T>;
        #endregion

        /// <summary>
        /// Apply a delegate d to this node and either its parents or children depending on the
        /// aType parameter.
        /// </summary>
        /// <typeparam name="U">Type of the node to apply d to.</typeparam>
        /// <param name="aType">Dictates type of recursion.</param>
        /// <param name="aStop">Dictates when to return.</param>
        /// <param name="d">The delegate to apply.</param>
        /// <returns>Whether the delegate was applied to a node.</returns>
        /// <remarks>
        /// It is safe for a delegate to modify a node's tree structure. This function will
        /// traverse the subtree that a node belongs to before the call to d(), not the subtree
        /// that might result after a call to d().
        /// </remarks>
        public bool Apply<U>(ApplyType aType, ApplyStop aStop, ApplyFunction<U> d) where U : TreeNode<T>
        {
            // Note: stored in the case that calling d() modifies this nodes hierarchical relationship.
            TreeNode<T> firstChild = mFirstChild;
            TreeNode<T> parent = mParent;

            if (this is U)
            {
                if (d((U)this) && (aStop == ApplyStop.Delegate))
                {
                    return true;
                }

                if (aStop == ApplyStop.First)
                {
                    return true;
                }
            }

            if (aType == ApplyType.RecurseDown)
            {
                for (TreeNode<T> child = firstChild; child != null; )
                {
                    // Note: done for the same reason as storing firstChild and parent.
                    TreeNode<T> t = child;
                    child = child.mNextSibling;

                    if (t.Apply<U>(aType, aStop, d))
                    {
                        return true;
                    }
                }
            }
            else if (aType == ApplyType.RecurseUp && parent != null)
            {
                return parent.Apply<U>(aType, aStop, d);
            }

            return false;
        }

        public T FirstChild
        {
            get
            {
                return mFirstChild;
            }
        }

        public T LastChild
        {
            get
            {
                return mLastChild;
            }
        }

#if DEBUG
        public int ChildCount
        {
            get
            {
                return mChildCount;
            }
        }
#endif

        /// <summary>
        /// Populates a list with all nodes of type U in the subtree of this node.
        /// </summary>
        /// <typeparam name="U">The type of node.</typeparam>
        /// <param name="aOut">The list to populate, should not be null.</param>
        public void GetAll<U>(List<U> aOut) where U : T
        {
            Apply<U>(ApplyType.RecurseDown, ApplyStop.Delegate, delegate(U e)
            {
                aOut.Add(e);
                return false;
            });
        }

        /// <summary>
        /// Returns the number of direct children of this node of type U.
        /// </summary>
        /// <typeparam name="U">The type of child.</typeparam>
        /// <returns>The number of direct children of type U.</returns>
        public int GetChildCount<U>() where U : T
        {
            int ret = 0;
            foreach (U e in GetEnumerable<U>())
            {
                ret++;
            }

            return ret;
        }

        public T NextSibling
        {
            get
            {
                return mNextSibling;
            }
        }

        public T PreviousSibling
        {
            get
            {
                return mPrevSibling;
            }
        }

        public virtual T Parent
        {
            get
            {
                return mParent;
            }

            set
            {
                if (value != mParent)
                {
                    _LeaveParent();

                    if (value != null)
                    {
                        value._AdoptChild((T)this);
                    }
                }
            }
        }

        #region Enumerable helpers
        public sealed class TreeEnumerator<U> where U : T
        {
            private U mCurrent = null;
            private U mNext = null;

            public TreeEnumerator(T aHead)
            {
                for (T t = aHead; t != null; t = t.mNextSibling)
                {
                    if (t is U)
                    {
                        mNext = (U)t;
                        break;
                    }
                }
            }

            public U Current
            {
                get
                {
                    return mCurrent;
                }
            }

            public bool MoveNext()
            {
                mCurrent = mNext;
                T t = mNext;

                while (t != null)
                {
                    t = t.mNextSibling;

                    if (t is U)
                    {
                        break;
                    }
                }

                mNext = (U)t;

                return (mCurrent != null);
            }
        }

        public struct EnumerateDummy<U> where U : T
        {
            #region Private members
            T mFirstChild;
            #endregion

            public EnumerateDummy(T aFirstChild)
            {
                mFirstChild = aFirstChild;
            }

            public TreeEnumerator<U> GetEnumerator()
            {
                return new TreeEnumerator<U>(mFirstChild);
            }
        }
        #endregion

        /// <summary>
        /// Returns an enumerable object that iterates over the direct children of this node
        /// of type U.
        /// </summary>
        /// <typeparam name="U">The type of child to iterate over.</typeparam>
        /// <returns>An enumerable object that can be iterated over with foreach.</returns>
        public EnumerateDummy<U> GetEnumerable<U>() where U : T
        {
            return new EnumerateDummy<U>(mFirstChild);
        }
    }
}
