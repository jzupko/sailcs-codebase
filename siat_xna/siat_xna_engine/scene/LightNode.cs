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
    /// A scene graph node that encapsulates an instance of a directional, point, or spot light.
    /// </summary>
    /// 
    /// \todo Directional lights do not cast shadows.
    /// \todo Point lights do not cast shadows - perhaps they never will. Might be beneficial to
    ///       keep point lights as the gauranteed "cheap" light type.
    public class LightNode : PoseableNode
    {
        public const float kNearPlaneScale = 4.38e-4f;
        public const float kFarPlaneScale = 1.0f;

        #region Protected members
        protected bool mbCastShadow = false;
        protected bool mbShadowsDirty = false;
        protected Light mLight = new Light();
        protected float mRange = Utilities.kMaxLightRange;
        protected int mShadowRenderTarget = -1;
        protected object mRangeBoxed = Utilities.kMaxLightRange;
        protected Matrix mShadowProjection = Matrix.Identity;
        protected MatrixWrapper mShadowViewWrapped = new MatrixWrapper();
        protected MatrixWrapper mShadowViewProjectionWrapped = new MatrixWrapper();
        protected Frustum mShadowWorldFrustum = new Frustum(Vector3.Zero, 6);
        protected Vector3 mWorldLightDirection = Vector3.Forward;

        protected void _PoseDirectionalPoint(IPoseable aPoseable)
        {
            #region Find intersecting planes
            Vector3 center = WorldPosition;
            int planeCount = Shared.ActiveWorldFrustum.Planes.Length;
            SiatPlane[] planes = Shared.ActiveWorldFrustum.Planes;
            Shared.ActiveLightPlanes = new SiatPlane[planeCount];

            for (int i = 0; i < planeCount; i++)
            {
                float d;
                planes[i].Plane.DotCoordinate(ref center, out d);
                Shared.ActiveLightPlanes[i] = planes[i];

                if (Utilities.GreaterThan(d, mRange)) { Shared.ActiveLightPlanes[i].Plane.D = d; }
            }
            #endregion

            Shared.ActiveShadowPlanes = null;
            aPoseable.LightingPose(this);
        }

        protected void _PoseSpot(IPoseable aPoseable)
        {
            if (mbCastShadow)
            {
                Shared.ActiveShadowPlanes = mShadowWorldFrustum.Planes;
                Shared.ActiveLightPlanes = Shared.ActiveWorldFrustum.Planes;
            }
            else
            {
                Shared.ActiveShadowPlanes = null;
                Shared.ActiveLightPlanes = Shared.ActiveWorldFrustum.Planes;
            }

            mbShadowsDirty = aPoseable.LightingPose(this);
        }

        protected void _ReleaseTarget()
        {
            if (mShadowRenderTarget >= 0)
            {
                ShadowMaps.Release(mShadowRenderTarget);
                mShadowRenderTarget = -1;
            }
        }
        #endregion

        #region Overrides
        public override BoundingBox AABB { get { return BoundingBox.CreateFromSphere(mWorldBounding); } }
        public override int FaceCount { get { return 0; } }

        public override bool bMyShadowRequiresUpdate
        {
            get { return false; }
        }

        public override void FrustumPose(IPoseable aPoseable)
        {
            if (aPoseable != null)
            {
                if (RenderRoot.bDeferredLighting && mLightMask == kDefaultMask) { RenderRoot.msDeferredLightList.Add(this); }

                if (mLight.Type == LightType.Spot) { _PoseSpot(aPoseable); }
                else { _PoseDirectionalPoint(aPoseable); }
            }
        }

        public override bool LightingPose(LightNode aLight)
        {
            return false;
        }

        protected override bool IsPoseable()
        {
            float diffuseL = Utilities.GetLuminance(ref mLight.LightDiffuse);
            float specularL = Utilities.GetLuminance(ref mLight.LightSpecular);

            bool bBrightEnough = Utilities.GreaterThan(diffuseL, Utilities.kMinLuminance) ||
                                 Utilities.AboutEqual(diffuseL, Utilities.kMinLuminance) ||
                                 Utilities.GreaterThan(specularL, Utilities.kMinLuminance) ||
                                 Utilities.AboutEqual(specularL, Utilities.kMinLuminance);

            bool bAttenuation = (mLight.Type == LightType.Directional) ? true : Utilities.GreaterThan(mLight.LightAttenuation.X + mLight.LightAttenuation.Y + mLight.LightAttenuation.Z, 0.0f, Utilities.kLooseToleranceFloat);
            bool bSpot = (mLight.Type != LightType.Spot) ? true : Utilities.LessThan(mLight.FalloffCosHalfAngle, 1.0f, Utilities.kLooseToleranceFloat);

            return (bBrightEnough && bAttenuation && bSpot && Utilities.GreaterThan(mRange, 0.0f, Utilities.kLooseToleranceFloat));
        }

        protected override void PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            LightNode l = (LightNode)aNode;

            l.mbCastShadow = mbCastShadow;
            l.mLight = mLight;
            l._SetPoseableDirty();
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new LightNode(aCloneId);
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);

            if (mbCastShadow)
            {
                if (mLight.Type != LightType.Spot)
                {
                    mbCastShadow = false;
                    _ReleaseTarget();
                }
                else if (mShadowRenderTarget < 0)
                {
                    mShadowRenderTarget = ShadowMaps.Grab();

                    if (mShadowRenderTarget >= 0) { mbShadowsDirty = true; }
                    else { mbCastShadow = false; }
                }
            }
            else if (mShadowRenderTarget >= 0)
            {
                _ReleaseTarget();
            }
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            #region Update range
            if (aCell != null)
            {
                float d = Utilities.GetLightRange(ref Light.LightAttenuation, ref Light.LightDiffuse, ref Light.LightSpecular);
                float d02 = (d * d);
                float d12 = Utilities.Min(Vector3.DistanceSquared(aCell.WorldBounding.Max, aCell.WorldBounding.Min), d02);
                float r2 = (mRange * mRange);

                if (d12 <= d02 && !Utilities.AboutEqual(d12, r2, Utilities.kLooseToleranceFloat))
                {
                    mRange = ((d12 > Utilities.kLooseToleranceFloat) ? (float)Math.Sqrt(d12) : 0.0f);
                    mRangeBoxed = mRange;
                    abChanged = true;
                }
            }
            #endregion

            if (abChanged)
            {
                #region Update bounding based on light range.
                if (Utilities.GreaterThan(mRange, 0.0f, Utilities.kLooseToleranceFloat))
                {
                    BoundingSphere bounding = new BoundingSphere(WorldPosition, mRange);

                    if (mbValidBounding) { BoundingSphere.CreateMerged(ref mWorldBounding, ref bounding, out mWorldBounding); }
                    else { mWorldBounding = bounding; mbValidBounding = true; }
                }
                #endregion

                #region Update light direction
                if (mLight.Type != LightType.Point)
                {
                    mWorldLightDirection = mITWorldWrapped.Matrix.Transform(Vector3.Forward);
                    mWorldLightDirection.Normalize();
                }
                else
                {
                    mWorldLightDirection = Vector3.Forward;
                }
                #endregion

                #region Update frustum and matrices
                if (mLight.Type == LightType.Spot && Utilities.GreaterThan(mRange, 0.0f, Utilities.kLooseToleranceFloat))
                {
                    float near = kNearPlaneScale * mRange;
                    float far = kFarPlaneScale * mRange;
                    mShadowProjection = Matrix.CreatePerspectiveFieldOfView(mLight.FalloffAngleInRadians, 1.0f, near, far);
                    Matrix.Invert(ref mWorldWrapped.Matrix, out mShadowViewWrapped.Matrix);
                    mShadowViewProjectionWrapped.Matrix = mShadowViewWrapped.Matrix * mShadowProjection;
                    mShadowWorldFrustum.Set(WorldPosition, ref mShadowViewProjectionWrapped.Matrix);
                }
                #endregion

                mbShadowsDirty = true;
            }

            base.PostUpdate(aCell, abChanged);
        }
        #endregion

        public LightNode() : base() { mFlags |= SceneNodeFlags.ExcludeFromBounding; }
        public LightNode(string aId) : base(aId) { mFlags |= SceneNodeFlags.ExcludeFromBounding; }
        ~LightNode() { _ReleaseTarget(); }

        public bool bCastShadow { get { return mbCastShadow; } set { mbCastShadow = value; } }
        public bool bShadowsDirty { get { return mbShadowsDirty; } set { mbShadowsDirty = value; } }

        public float Range { get { return mRange; } }
        public object RangeBoxed { get { return mRangeBoxed; } }
        public Matrix ShadowProjection { get { return mShadowProjection; } }
        public Matrix ShadowView { get { return mShadowViewWrapped.Matrix; } }
        public MatrixWrapper ShadowViewWrapped { get { return mShadowViewWrapped; } }
        public Matrix ShadowViewProjection { get { return mShadowViewProjectionWrapped.Matrix; } }
        public MatrixWrapper ShadowViewProjectionWrapped { get { return mShadowViewProjectionWrapped; } }
        public Vector3 WorldLightDirection { get { return mWorldLightDirection; } }

        public RenderRoot.RenderTargetPackage ShadowRenderTarget
        {
            get
            {
                if (mShadowRenderTarget >= 0) { return ShadowMaps.Get(mShadowRenderTarget); }
                else { return null; }
            }
        }

        public Light Light
        {
            get { return mLight; }
            set
            {
                if (value != mLight)
                {
                    if (value == null) { throw new Exception("Light of LightNode cannot be null."); }

                    mLight = value;
                    mFlags |= SceneNodeFlags.LocalDirty;
                    _SetPoseableDirty();
                }
            }
        }

    }
}
