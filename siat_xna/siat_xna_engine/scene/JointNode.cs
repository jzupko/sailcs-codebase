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
using System.Text;
using siat.render;

namespace siat.scene
{
    /// <summary>
    /// A scene node that encapsulates a skeletal piece in a animateable bone hiearchy.
    /// </summary>
    public sealed class JointNode : SceneNode
    {
        #region Private members
        private Animation mAnimation = null;
        internal AnimationControl mAnimationControl = null;
        #endregion

        #region Overrides
        protected override void PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            JointNode joint = (JointNode)aNode;
            joint.mAnimation = mAnimation;
            joint.mAnimationControl = mAnimationControl;
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new JointNode(aCloneId);
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            if (mParent != null && !(mParent is JointNode)) { mFlags |= SceneNodeFlags.IgnoreParent; }
            else { mFlags &= ~SceneNodeFlags.IgnoreParent; }

            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);

            if (mAnimationControl != null && mAnimationControl.Tick(mAnimation, ref mLocal))
            {
                mFlags |= SceneNodeFlags.LocalDirty;
            }
        }
        #endregion

        public JointNode() : base() { }
        public JointNode(string aId) : base(aId) { }

        public Animation Animation { get { return mAnimation; } set { mAnimation = value; } }
    }
}
