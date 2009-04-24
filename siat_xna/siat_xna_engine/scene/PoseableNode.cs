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

namespace siat.scene
{
    /// <summary>
    /// Base node for scene graph nodes that can be posed.
    /// </summary>
    /// <remarks>
    /// Posing can be metaphorically thought of as "posing for a picture". This is the pass where
    /// any nodes that wish to be rendered prepare for rendering, usually by calling a function
    /// in RenderRoot.PoseOperations which in-turn adds operations to a render tree to be executed
    /// during the Siat.Draw() pass.
    /// </remarks>
    /// 
    /// \sa siat.Siat 
    /// \sa siat.render.RenderRoot
    /// \sa siat.render.RenderRoot.PoseOperations
    /// \sa siat.render.RenderRoot.RenderOperations
    /// \sa siat.render.RenderNode
    /// 
    /// \todo I would like to decouple membership in a kdTree from PoseableNode.
    public abstract class PoseableNode : SceneNode, IPoseable, IkdTreeObject
    {
        public const UInt64 kDefaultMask = (1 << 0);

        #region Private members
        private bool mbEnablePosing = true;

        private void _UpdatePoseable(Cell aCell, bool abChanged)
        {
            if (aCell != null)
            {
                if (abChanged || (mFlags & SceneNodeFlags.PoseableDirty) != 0)
                {
                    bool bPoseable = mbEnablePosing && IsPoseable();

                    if (mThis.IsValid)
                    {
                        if (bPoseable)
                        {
                            aCell.Update(this);
                        }
                        else
                        {
                            mThis.Remove();
                        }
                    }
                    else if (bPoseable)
                    {
                        aCell.Add(this);
                    }

                    mFlags &= ~SceneNodeFlags.PoseableDirty;
                }
            }
        }
        #endregion

        #region Protected members
        protected UInt64 mLightMask = kDefaultMask;
        protected UInt64 mShadowMask = kDefaultMask;

        protected void _SetPoseableDirty()
        {
            mFlags |= SceneNodeFlags.PoseableDirty;
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            _UpdatePoseable(aCell, abChanged);

            base.PostUpdate(aCell, abChanged);
        }

        protected PoseableNode() : base() { mThis = OcclusionKdTree.PoseableEntry.Invalid; }
        protected PoseableNode(string aId) : base(aId) { mThis = OcclusionKdTree.PoseableEntry.Invalid; }
        #endregion

        #region Internal members
        internal OcclusionKdTree.PoseableEntry mThis;
        #endregion

        ~PoseableNode()
        {
            if (mThis.IsValid) { mThis.Remove(); }
        }

        protected abstract bool IsPoseable();

        public abstract bool bMyShadowRequiresUpdate { get; }
        public virtual void FrustumPose(IPoseable aPoseable) { }
        public virtual bool LightingPose(LightNode aLight) { return bDirty; }
        public virtual void Pick(Cell aCell, ref Ray aWorldRay) { }

        /// <summary>
        /// This pose applies to a PoseableNode that will cast shadow from LightNode aLight. 
        /// </summary>
        /// <param name="aLight">The light that initiated this pose.</param>
        public virtual void ShadowingPose(LightNode aLight) { }

        public bool bEnablePosing
        {
            get
            {
                return mbEnablePosing;
            }
            
            set
            {
                if (value != mbEnablePosing)
                {
                    mbEnablePosing = value;
                    mFlags |= SceneNodeFlags.PoseableDirty;

                    Apply<PoseableNode>(ApplyType.RecurseDown, ApplyStop.Delegate, delegate(PoseableNode e)
                        {
                            e.mbEnablePosing = value;
                            e.mFlags |= SceneNodeFlags.PoseableDirty;
                            return false;
                        });
                }
            }
        }

        public bool IsPosed { get { return mThis.IsValid; } }

        public abstract BoundingBox AABB { get; }
        public abstract int FaceCount { get; }

        public UInt64 LightMask { get { return mLightMask; } set { mLightMask = value; } }
        public UInt64 ShadowMask { get { return mShadowMask; } set { mShadowMask = value; } }
    }
}
