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
using System.Collections.Generic;
using siat;

namespace jz.physics.narrowphase
{
    /// <summary>
    /// Defines grouping of rigid bodies.
    /// </summary>
    [Flags]
    public enum BodyFlags
    {
        kNone = 0,
        kDynamic = (1 << 0),
        kKinematic = (1 << 1),
        kStatic = (1 << 2),
    }

    public abstract class Body 
    {
        #region Private members
        private BodyFlags mCollidesWith = BodyFlags.kDynamic;
        private BodyFlags mType = BodyFlags.kStatic;
        private World mWorld = null;
        #endregion

        #region Protected members
        protected void _UpdateWorldAABB()
        {
            if (mWorld != null)
            {
                BoundingBox worldAABB;
                CoordinateFrame.Transform(ref mLocalAABB, ref mFrame, out worldAABB);
                mWorld.Update(this, ref worldAABB);
            }
        }

        protected Body(BodyFlags aType, BodyFlags aCollidesWith)
        {
            mType = aType;
            mCollidesWith = aCollidesWith;
        }
        #endregion

        #region Internal members
        internal BoundingBox mLocalAABB = Utilities.kZeroBox;
        internal CoordinateFrame mFrame = CoordinateFrame.Identity;
        internal CoordinateFrame mPrevFrame = CoordinateFrame.Identity;
        internal ushort mHandle = ushort.MaxValue;
        #endregion

        public virtual void Apply(Body b, ContactPoint aPoint) { }

        public CoordinateFrame Frame { get { return mFrame; } set { mPrevFrame = mFrame;  mFrame = value; _UpdateWorldAABB(); } }
        public BoundingBox LocalAABB { get { return mLocalAABB; } }

        public virtual BodyFlags CollidesWith
        {
            get { return mCollidesWith; }
            set
            {
                mCollidesWith = value;

                if (mWorld != null) { mWorld.UpdateType(this); }
            }
        }

        public virtual BodyFlags Type
        {
            get { return mType; }
            set
            {
                mType = value;

                if (mWorld != null) { mWorld.UpdateType(this); }
            }
        }

        public virtual World World
        {
            get { return mWorld; }
            set
            {
                if (mWorld != null) { mWorld.Remove(this); }
                mWorld = value;
                if (mWorld != null) { mWorld.Add(this); }
            }
        }
    }
}
