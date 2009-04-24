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
    /// A scene node that encapsulates an instance of animateable geometry.
    /// </summary>
    /// 
    /// \todo Bounding volumes for animated mesh parts are currently not correct. They are not
    ///       calculated for each frame of animation. The current hack is to apply the root joint
    ///       node transform to the bounding volume.
    public class AnimatedMeshPartNode : MeshPartNode
    {
        #region Protected members
        private AnimationControl mAnimationControl = new AnimationControl();
        private Matrix mBind = Matrix.Identity;
        protected Matrix[] mInvBinds = new Matrix[0];
        protected JointNode[] mJoints = new JointNode[0];
        protected string[] mJointIds = new string[0];
        protected bool mbJointsDirty = false;
        protected int mRootIndex = -1;
        protected JointNode mRootJoint = null;
        protected string mRootJointId = string.Empty;
        protected Vector4[] mSkinning = new Vector4[0];

        protected void _JointRetrieveHelper(string aId, int aIndex)
        {
            Retrieve(aId, delegate(SceneNode e)
            {
                mJoints[aIndex] = (JointNode)e;
                mJoints[aIndex].mAnimationControl = mAnimationControl;
            });
        }
        #endregion

        #region Overrides
        public override void FrustumPose(IPoseable aPoseable)
        {
            if (mEffect.IsAnimatedBase)
            {
                RenderRoot.PoseOperations.AnimatedMeshPartBase(mWorldWrapped, mITWorldWrapped, mSkinning, mViewDepth, mMeshPart, mMaterial, mEffect, (mLightMask == kDefaultMask && !bExcludeFromShadowing));
            }

            if (mbDrawBoundingBox)
            {
                RenderRoot.PoseOperations.WireframeBox(mBoxDrawMatrixWrapped);
            }
        }

        public override bool LightingPose(LightNode aLight)
        {
            if (mEffect.IsAnimatedLightable)
            {
                RenderRoot.PoseOperations.AnimatedMeshPartLit(mWorldWrapped, mITWorldWrapped, mSkinning, 
                    mViewDepth, mMeshPart, mMaterial, mEffect, aLight,
                    (aLight.bCastShadow && !bExcludeFromShadowing), (mLightMask == kDefaultMask && !bExcludeFromShadowing));
            }

            return bDirty;
        }

        public override void ShadowingPose(LightNode aLight)
        {
            if (mEffect.IsAnimatedLightable)
            {
                RenderRoot.PoseOperations.AnimatedMeshPartShadow(mWorldWrapped, mSkinning, mViewDepth, mMeshPart, aLight);
            }
        }

        protected override bool IsPoseable()
        {
            return (mEffect != null && mMeshPart != null && mSkinning.Length > 0);
        }

        protected override void PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            AnimatedMeshPartNode clone = (AnimatedMeshPartNode)aNode;
            clone.mAnimationControl = mAnimationControl;
            clone.mBind = mBind;
            clone.mbJointsDirty = mbJointsDirty;
            Array.Resize(ref clone.mInvBinds, mInvBinds.Length); mInvBinds.CopyTo(clone.mInvBinds, 0);
            Array.Resize(ref clone.mJoints, mJoints.Length); mJoints.CopyTo(clone.mJoints, 0);
            Array.Resize(ref clone.mJointIds, mJointIds.Length); mJoints.CopyTo(clone.mJointIds, 0);
            clone.mRootJoint = mRootJoint;
            clone.mRootJointId = mRootJointId;
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new AnimatedMeshPartNode(aCloneId);
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            if (mbJointsDirty)
            {
                int count = mJointIds.Length;
                Array.Resize<JointNode>(ref mJoints, count);
                Array.Resize<Vector4>(ref mSkinning, (count * 3));
                Array.Clear(mJoints, 0, count);

                for (int i = 0; i < count; i++) { _JointRetrieveHelper(mJointIds[i], i); }

                Retrieve(mRootJointId, delegate(SceneNode e)
                {
                    mRootJoint = (JointNode)e;
                    mRootJoint.mAnimationControl = mAnimationControl;

                    for (int i = 0; i < mJointIds.Length; i++)
                    {
                        if (mJoints[i] == mRootJoint) { mRootIndex = i; break; }
                    }
                });

                mbJointsDirty = false;
            }

            if (mRootJoint != null && mRootJoint.bDirty)
            {
                int count = mJoints.Length;
                for (int i = 0; i < count; i++)
                {
                    int entryIndex = (i * 3);

                    if (mJoints[i] != null)
                    {
                        Matrix transform = mBind * mInvBinds[i] * mJoints[i].WorldTransform;

                        mSkinning[entryIndex + 0] = new Vector4(transform.M11, transform.M21, transform.M31, transform.M41);
                        mSkinning[entryIndex + 1] = new Vector4(transform.M12, transform.M22, transform.M32, transform.M42);
                        mSkinning[entryIndex + 2] = new Vector4(transform.M13, transform.M23, transform.M33, transform.M43);
                    }
                    else
                    {
                        mSkinning[entryIndex + 0] = Vector4.UnitX;
                        mSkinning[entryIndex + 1] = Vector4.UnitY;
                        mSkinning[entryIndex + 2] = Vector4.UnitZ;
                    }
                }
            }

            if (mRootIndex >= 0)
            {
                Matrix m = mWorldWrapped.Matrix;
                mWorldWrapped.Matrix = mBind * mInvBinds[mRootIndex] * mRootJoint.WorldTransform * m;
                base.PostUpdate(aCell, abChanged);
                mWorldWrapped.Matrix = m;
            }
            else
            {
                base.PostUpdate(aCell, abChanged);
            }
        }


        public override void Pick(Cell aCell, ref Ray aWorldRay)
        {
            if (mbPickable)
            {
                RenderRoot.PoseOperations.AnimatedPicking(mWorldWrapped, mSkinning, mViewDepth, mMeshPart, mMaterial, mEffect, Siat.Singleton.GetPickingColor(aCell, this));
            }
        }
        #endregion

        public AnimatedMeshPartNode() : base() {}
        public AnimatedMeshPartNode(string aId) : base(aId) {}

        public AnimationControl AnimationControl { get { return mAnimationControl; } }
        public Matrix BindTransform { get { return mBind; } set { mBind = value; } }
        public Matrix[] InvJointBindTransforms { get { return mInvBinds; } set { mInvBinds = value; } }
        public string[] JointIds { get { return mJointIds; } set { mJointIds = value; mbJointsDirty = true; } }
        public string RootJointId { get { return mRootJointId; } set { mRootJointId = value; mbJointsDirty = true; } }
    }

}
