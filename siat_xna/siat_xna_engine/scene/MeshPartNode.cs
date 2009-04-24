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
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using siat.render;

namespace siat.scene
{
    /// <summary>
    /// A scene graph node that encapsulates an instance of non-animating geometry.
    /// </summary>
    public class MeshPartNode : PoseableNode
    {
        #region Private members
        private void _ValidateMaterial()
        {
            if (mMaterial != null && mEffect != null)
            {
                if (!mMaterial.Validate(mEffect))
                {
#if DEBUG
                    List<string> validateFail = new List<string>();
                    mMaterial.GetValidateFail(mEffect, validateFail);

                    string message = "Material for effect \"" + mEffect.Id + "\" failed validation " +
                        "because the following parameters by semantic are not present in the effect:" +
                        Environment.NewLine;

                    foreach (string e in validateFail)
                    {
                        message += "\"" + e + "\"" + Environment.NewLine;
                    }

                    throw new Exception(message);
#else
                    mMaterial = null;
#endif
                }
            }
        }
        #endregion

        #region Protected members
        protected MatrixWrapper mBoxDrawMatrixWrapped = new MatrixWrapper();
        protected bool mbDrawBoundingBox = false;
        protected bool mbPickable = false;
        protected SiatEffect mEffect = null;
        protected uint mLastTick = 0;
        protected SiatMaterial mMaterial = null;
        protected MeshPart mMeshPart = null;
        protected float mViewDepth = 0.0f;
        protected BoundingBox mWorldAABB = Utilities.kZeroBox;
        #endregion

        #region Overrides
        public override BoundingBox AABB { get { return mWorldAABB; } }
        public override int FaceCount { get { return (mMeshPart != null) ? mMeshPart.PrimitiveCount : 0; } }

        public override bool bMyShadowRequiresUpdate
        {
            get 
            {
                return bDirty;
            }
        }

        public override void FrustumPose(IPoseable aPoseable)
        {
            uint current = Siat.Singleton.FrameTick;

            if (mLastTick != current)
            {
                mLastTick = current;

                if (mEffect.IsStandardBase)
                {
                    RenderRoot.PoseOperations.MeshPartBase(mWorldWrapped, mITWorldWrapped, mViewDepth, mMeshPart, mMaterial, mEffect, (mLightMask == kDefaultMask && !bExcludeFromShadowing));
                }

                if (mbDrawBoundingBox)
                {
                    RenderRoot.PoseOperations.WireframeBox(mBoxDrawMatrixWrapped);
                }
            }
        }

        public override bool LightingPose(LightNode aLight)
        {
            if (mEffect.IsStandardLightable)
            {
                RenderRoot.PoseOperations.MeshPartLit(mWorldWrapped, mITWorldWrapped,
                    mViewDepth, mMeshPart, mMaterial, mEffect, aLight, (aLight.bCastShadow && !bExcludeFromShadowing), (mLightMask == kDefaultMask && !bExcludeFromShadowing));
            }

            return bDirty;
        }

        public override void ShadowingPose(LightNode aLight)
        {
            if (mEffect.IsStandardLightable)
            {
                RenderRoot.PoseOperations.MeshPartShadow(mWorldWrapped, mViewDepth, mMeshPart, aLight);
            }
        }

        protected override bool IsPoseable()
        {
            return (mEffect != null && mMeshPart != null);
        }

        protected override void PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            MeshPartNode m = (MeshPartNode)aNode;

            m.mbDrawBoundingBox = mbDrawBoundingBox;
            m.Effect = mEffect;
            m.mMaterial = mMaterial;
            m.MeshPart = mMeshPart;
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new MeshPartNode(aCloneId);
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            // Todo: find a better way to validate the material, this is a bit expensive to be doing every
            // update for every meshpart.
            _ValidateMaterial();

            if (abChanged)
            {
                #region Update bounding based on mesh part.
                if (mMeshPart != null)
                {
                    BoundingSphere localBounding = mMeshPart.BoundingSphere;
                    BoundingSphere bounding;

                    // Note: BoundingSphere.Transform() is unreliable. It produces very odd values
                    //  (negative radii or too small radii) when matrices have rotation or negative
                    //  scaling coefficients in them. 
                    // localBounding.Transform(ref localBounding, out bounding);
                    Utilities.Transform(ref localBounding, ref mWorldWrapped.Matrix, out bounding);

                    if (mbValidBounding)
                    {
                        BoundingSphere.CreateMerged(ref mWorldBounding, ref bounding, out mWorldBounding);
                    }
                    else
                    {
                        mWorldBounding = bounding;
                        mbValidBounding = true;
                    }

                    Utilities.Transform(ref mMeshPart.AABB, ref mWorldWrapped.Matrix, out mWorldAABB);
                }
                #endregion

                mBoxDrawMatrixWrapped.Matrix = Matrix.CreateScale(Utilities.GetHalfExtents(mWorldAABB)) * Matrix.CreateTranslation(Utilities.GetCenter(mWorldAABB));
            }

            #region Update view sorting depth.
            if (mMeshPart != null)
            {
                Matrix m = mWorldWrapped.Matrix * Shared.ViewTransform;
                mViewDepth = Vector3.Transform(Utilities.GetCenter(mMeshPart.AABB), m).Z;
            }
            else
            {
                mViewDepth = Vector3.Transform(WorldPosition, Shared.ViewTransform).Z;
            }
            #endregion
            
            base.PostUpdate(aCell, abChanged);
        }

        public override void Pick(Cell aCell, ref Ray aWorldRay)
        {
            if (mbPickable)
            {
                RenderRoot.PoseOperations.Picking(mWorldWrapped, mViewDepth, mMeshPart, mMaterial, mEffect, Siat.Singleton.GetPickingColor(aCell, this));
            }
        }
        #endregion

        public MeshPartNode() : base() { }
        public MeshPartNode(string aId) : base(aId) { }

        public bool bDrawBoundingBox { get { return mbDrawBoundingBox; } set { mbDrawBoundingBox = value; } }
        public bool bExcludeFromShadowing
        {
            get
            {
                return (mFlags & SceneNodeFlags.ExcludeFromShadowing) != 0;
            }

            set
            {
                if (value) { mFlags |= SceneNodeFlags.ExcludeFromShadowing; }
                else { mFlags &= ~SceneNodeFlags.ExcludeFromShadowing; }
            }
        }

        public MeshPart MeshPart
        {
            get
            {
                return mMeshPart;
            }
            
            set
            {
                mMeshPart = value;

                mFlags |= SceneNodeFlags.LocalDirty;
                _SetPoseableDirty();
            }
        }

        public SiatEffect Effect
        {
            get
            {
                return mEffect;
            }

            set
            {
                if (value != mEffect)
                {
                    mEffect = value;

                    if (mEffect != null)
                    {
                        mbPickable = (mEffect.GetTechnique(RenderRoot.BuiltInTechniques.siat_RenderPicking) != null);
                    }

                    _SetPoseableDirty();
                }
            }
        }

        public SiatMaterial Material
        {
            get
            {
                return mMaterial;
            }

            set
            {
                if (value != mMaterial)
                {
                    mMaterial = value;
                }
            }
        }
    }
}
