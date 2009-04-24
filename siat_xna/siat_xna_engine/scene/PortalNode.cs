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
    /// A scene graph node that encapsulates an instance of a portal.
    /// </summary>
    /// <remarks>
    /// Portals are planar windows through which one cell can see another. Cells have no spatial relationship
    /// withou a connection through portals.
    /// </remarks>
    /// 
    /// \todo Portals can be arbitrary planar geometry but the content pipeline currently flattens everything to
    ///       a quadrilateral. This should be fixed.
    public class PortalNode : PoseableNode
    {
        #region Protected members
        protected Matrix mDestinationTransform = Matrix.Identity;
        protected Matrix mInverseDestinationTransform = Matrix.Identity;
        protected Portal mPortal = null;
        protected string mPortalToCell = string.Empty;
        protected string mPortalToNode = string.Empty;
        protected Cell mToCell = null;
        protected Matrix mToCellToWorld = Matrix.Identity;
        protected SceneNode mToNode = null;
        protected Plane mWorldPlane = new Plane(Vector3.Up, 0.0f);
        protected Vector3[] mWorldPositions = new Vector3[0];

        protected bool _Clip(out Frustum arReducedFrustum, out Vector3[] arPositions)
        {
            Siat siat = Siat.Singleton;

            Vector3[] newPositions;

            if (Utilities.Clip(mWorldPositions, Shared.ActiveWorldFrustum.Planes, out newPositions) == 0)
            {
                arPositions = null;
                arReducedFrustum = default(Frustum);
                return false;
            }
            else
            {
                int vertexCount = newPositions.Length;
                int planeCount = vertexCount + 2;
                int prev = vertexCount - 1;

                Frustum reducedFrustum = new Frustum(Shared.ActiveWorldFrustum.Center, planeCount);
                reducedFrustum.Planes[Frustum.kNear].Plane = new Plane(-mWorldPlane.Normal, -mWorldPlane.D);
                reducedFrustum.Planes[Frustum.kNear].UpdateAbsNormal();
                reducedFrustum.Planes[Frustum.kFar] = Shared.ActiveWorldFrustum.Planes[Frustum.kFar];

                for (int i = 0; i < vertexCount; prev = i, i++)
                {
                    reducedFrustum.Planes[i + 2].Plane = new Plane(reducedFrustum.Center, newPositions[prev], newPositions[i]);
                    reducedFrustum.Planes[i + 2].UpdateAbsNormal();
                }

                arPositions = newPositions;
                arReducedFrustum = reducedFrustum;
                return true;
            }
        }

        #region Update handler
        protected void __UpdatedHandler(Cell aCell, SceneNode aNode)
        {
            mDestinationTransform = mWorldWrapped.Matrix;
            mDestinationTransform.Translation = (aCell != null) ? Vector3.Transform(aNode.WorldTransform.Translation, aCell.InverseCellToWorld) : aNode.WorldTransform.Translation;
            
            Matrix.Invert(ref mDestinationTransform, out mInverseDestinationTransform);
            mToCellToWorld = mInverseDestinationTransform * mWorldWrapped.Matrix;
        }
        protected Callback _UpdateHandler;
        #endregion
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
            Siat siat = Siat.Singleton;
            Vector3 center = Shared.ActiveWorldFrustum.Center;

            if (Utilities.Intersect(ref mWorldPlane, ref center) == PlaneIntersectionType.Front)
            {
                Vector3[] clippedPositions;
                Frustum reducedFrustum;

                if (_Clip(out reducedFrustum, out clippedPositions))
                {
                    Frustum oldFrustum = Shared.ActiveWorldFrustum;
                    Shared.ActiveWorldFrustum = reducedFrustum;
                    {
                        mToCell.FrustumPose(aPoseable);
                    }
                    Shared.ActiveWorldFrustum = oldFrustum;
                }
            }
        }

        protected override bool IsPoseable()
        {
            return (mToCell != null && mParent != null);
        }

        protected override void PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            PortalNode p = (PortalNode)aNode;
            p.mDestinationTransform = mDestinationTransform;
            p.mInverseDestinationTransform = mInverseDestinationTransform;
            p.mPortal = mPortal;
            p.mPortalToCell = mPortalToCell;
            p.mPortalToNode = mPortalToNode;
            p.mToCell = mToCell;
            p.mToCellToWorld = mToCellToWorld;
            p.mToNode = mToNode;
            p.mWorldPlane = mWorldPlane;
            p.mWorldPositions = mWorldPositions;
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new PortalNode(aCloneId);
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);

            if (mToNode == null && mPortalToNode != string.Empty)
            {
                if (mToCell != null)
                {
                    SceneNode root = mToCell.RootSceneNode;
                    if (root != null)
                    {
                        mToNode = SceneNode.Find<SceneNode>(mPortalToNode);
                        if (mToNode != null)
                        {
                            mToNode.OnUpdateEnd += _UpdateHandler;
                            _UpdateHandler(mToCell, mToNode);
                        }
                    }
                }
            }
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            if (abChanged)
            {
                #region Update portal vertices, normal, and bounding.
                if (mPortal != null)
                {
                    int count = mWorldPositions.Length;
                    Vector3[] localPositions = mPortal.LocalPositions;

                    for (int i = 0; i < count; i++)
                    {
                        Vector3.Transform(ref localPositions[i], ref mWorldWrapped.Matrix, out mWorldPositions[i]);
                    }

                    mWorldPlane = new Plane(mWorldPositions[0], mWorldPositions[2], mWorldPositions[1]);

                    BoundingSphere bounding = BoundingSphere.CreateFromPoints(mWorldPositions);

                    if (mbValidBounding)
                    {
                        BoundingSphere.CreateMerged(ref mWorldBounding, ref bounding, out mWorldBounding);
                    }
                    else
                    {
                        mWorldBounding = bounding;
                        mbValidBounding = true;
                    }
                }
                #endregion

                mToCellToWorld = mInverseDestinationTransform * mWorldWrapped.Matrix;
            }

            #region Update ToCell
            if (mToCell != null)
            {
                mToCell.Update(ref mToCellToWorld);
            }
            #endregion

            base.PostUpdate(aCell, abChanged);
        }

        public override void Pick(Cell aCell, ref Ray aWorldRay)
        {
            if (Utilities.GreaterThan(mWorldPlane.DotCoordinate(aWorldRay.Position), 0.0f))
            {
                mToCell.Pick(ref aWorldRay);
            }
        }
        #endregion

        const char kDelimiter = '#';

        public PortalNode() : base() { _UpdateHandler = __UpdatedHandler; mFlags |= SceneNodeFlags.ExcludeFromBounding; }
        public PortalNode(string aId) : base(aId) { _UpdateHandler = __UpdatedHandler; mFlags |= SceneNodeFlags.ExcludeFromBounding; }

        public Cell ToCell { get { return mToCell; } }
        public SceneNode ToNode { get { return mToNode; } }
        public Plane WorldPlane { get { return mWorldPlane; } }
        public Vector3[] WorldPositions { get { return mWorldPositions; } }

        public Portal Portal
        {
            get
            {
                return mPortal;
            }

            set
            {
                if (value != mPortal)
                {
                    mPortal = value;

                    if (mPortal == null)
                    {
                        mWorldPositions = new Vector3[0];
                    }
                    else
                    {
                        mWorldPositions = new Vector3[mPortal.LocalPositions.Length];
                    }

                    _SetPoseableDirty();
                }
            }
        }

        public string PortalTo
        {
            get
            {
                if (mPortalToCell == string.Empty) { return string.Empty; }
                else { return mPortalToCell + kDelimiter + mPortalToNode; }
            }

            set
            {
                string[] split = value.Split(kDelimiter);

                if (split.Length > 0)
                {
                    mPortalToCell = Utilities.RemoveExtension(split[0]);

                    if (mPortalToCell != string.Empty) { mToCell = Cell.GetCell(mPortalToCell); }
                }

                if (split.Length == 2)
                {
                    mPortalToNode = split[1];
                    if (mToNode != null)
                    {
                        mToNode.OnUpdateEnd -= _UpdateHandler;
                        mToNode = null;
                    }
                }
            }
        }


    }
}
