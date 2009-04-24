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
using System.Diagnostics;
using siat.render;

namespace siat.scene
{
    /// <summary>
    /// A kdTree that also uses hardware occlusion queries to check for occlusion before traversing.
    /// </summary>
    /// \todo Hardware occlusion queries are apparently not thread-safe - this is managed in Cell by building
    ///       the kdTree in the main thread. However, this is not explicitly enforced but should be in order to
    ///       avoid bugs from accidentally updating of the kd-Tree in a secondary thread.
    public sealed class OcclusionKdTree : kdTree<PoseableNode>
    {
        public const int kMinimumPixelCountForNotOccluded = 1;
        public const float kMinimumFactorForOcclusionQuery = 0.05f;

        #region Private members
        private struct OcclusionEntry
        {
            public OcclusionEntry(OcclusionQuery aQuery)
            {
                Query = aQuery;
                LastFrameTick = 0;
                bLastOcclusionCheck = false;
                bUpdateOcclusion = true;
            }

            public OcclusionQuery Query;
            public uint LastFrameTick;
            public bool bLastOcclusionCheck;
            public bool bUpdateOcclusion;
        }

        OcclusionEntry[] mQueries;
        MatrixWrapper[] mWorldWrapped;

        private bool _IsOccluded(int i)
        {
            if (mQueries[i].Query == null) { return false; }
            else { return mQueries[i].bLastOcclusionCheck; }
        }

        private void _Remove(int aIndex, int aSubIndex)
        {
            List<PoseableNode> objects = mNodes[aIndex].Objects;
            objects.RemoveAt(aSubIndex);

            #region Refresh sub indices.
            int count = objects.Count;
            for (int i = aSubIndex; i < count; i++)
            {
                objects[i].mThis = new PoseableEntry(this, aIndex, i);
            }
            #endregion
        }

        private void _UpdateEntries()
        {
            for (int i = 0; i < mNodeCount; i++)
            {
                #region Update transforms
                mWorldWrapped[i] = new MatrixWrapper(
                    Matrix.CreateScale(Utilities.GetHalfExtents(ref mNodes[i].AABB)) *
                    Matrix.CreateTranslation(Utilities.GetCenter(ref mNodes[i].AABB)));
                #endregion

                #region Update This handles
                List<PoseableNode> list = mNodes[i].Objects;
                int count = list.Count;
                for (int j = 0; j < count; j++)
                {
                    list[j].mThis = new PoseableEntry(this, i, j);
                }
                #endregion
            }
        }
        #endregion

        #region PoseableEntry
        public struct PoseableEntry
        {
            #region Private members
            public OcclusionKdTree mKdTree;
            #endregion

            public PoseableEntry(OcclusionKdTree aTree, int aIndex, int aSubIndex)
            {
                mKdTree = aTree;
                Index = aIndex;
                SubIndex = aSubIndex;
            }

            public static PoseableEntry Invalid { get { return new PoseableEntry(null, -1, -1); } }

            public bool IsValid { get { return (mKdTree != null); } }
            public void Remove()
            {
                Utilities.Assert(IsValid);

                if (IsValid)
                {
                    mKdTree._Remove(Index, SubIndex);
                    mKdTree = null;
                }
            }

            public readonly int Index;
            public readonly int SubIndex;
        }
        #endregion

#if DEBUG
        public int TotalQueriesIssued = 0;
#endif

        public OcclusionKdTree() : this(DefaultCoefficients) { }
        public OcclusionKdTree(int aDepth) : this(DefaultCoefficients, aDepth) { }
        public OcclusionKdTree(kdTreeCoefficients aCoeff) : this(aCoeff, kMinimumDepth) { }
        public OcclusionKdTree(kdTreeCoefficients aCoeff, int aDepth)
            : base(aCoeff, aDepth)
        {
            mQueries = new OcclusionEntry[mNodes.Length];
            mWorldWrapped = new MatrixWrapper[mNodes.Length];
        }

        public void Add(PoseableNode aNode)
        {
            BoundingBox aabb = aNode.AABB;

            // Initializing to 0 is important - it's possible something will be inserted that is bigger
            // then the largest bounds of the kdTree. In this case, it should always be inserted into
            // the root node.
            int insertIndex = 0;

            for (int i = 0; i < mNodeCount; )
            {
                bool bContains = (mNodes[i].AABB.Contains(aabb) == ContainmentType.Contains);

                if (bContains)
                {
                    insertIndex = i;

                    if (_IsLeaf(insertIndex))
                    {
                        break;
                    }
                }

                i = _Next(bContains, i);
            }

            mNodes[insertIndex].Objects.Add(aNode);
            aNode.mThis = new PoseableEntry(this, insertIndex, mNodes[insertIndex].Objects.Count - 1);
        }

        public void Build()
        {
            List<PoseableNode> buildObjects = new List<PoseableNode>();
            List<PoseableNode> nonBuildObjects = new List<PoseableNode>();

            for (int i = 0; i < mNodeCount; i++)
            {
                List<PoseableNode> list = mNodes[i].Objects;
                int count = list.Count;

                for (int j = 0; j < count; j++)
                {
                    PoseableNode entry = list[j];
                    if ((entry.Flags & SceneNodeFlags.ExcludeFromBounding) != 0)
                    {
                        nonBuildObjects.Add(entry);
                    }
                    else
                    {
                        buildObjects.Add(entry);
                    }
                }

                list.Clear();
            }

            _Build(buildObjects);

            Array.Resize(ref mQueries, mNodeCount);
            Array.Resize(ref mWorldWrapped, mNodeCount);

            _UpdateEntries();

            int nonBuildCount = nonBuildObjects.Count;
            for (int i = 0; i < nonBuildCount; i++)
            {
                Add(nonBuildObjects[i]);
            }
        }

        public void FrustumPose(IPoseable aPoseable)
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = (Shared.ActiveWorldFrustum.Contains(ref mNodes[i].AABB) != ContainmentType.Disjoint) && !_IsOccluded(i);

                if (bIntersects)
                {
                    List<PoseableNode> list = mNodes[i].Objects;

                    int count = list.Count;
                    for (int j = 0; j < count; j++)
                    {
                        BoundingBox box = list[j].AABB;
                        if (Shared.ActiveWorldFrustum.Contains(ref box) != ContainmentType.Disjoint)
                        {
                            list[j].FrustumPose(aPoseable);
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }

        public bool LightingPose(LightNode aLight)
        {
            bool bReturn = false;

            if (aLight.bCastShadow)
            {
                for (int i = 0; i < mNodeCount; )
                {
                    bool bIntersects = (Utilities.Contains(Shared.ActiveShadowPlanes, ref mNodes[i].AABB) != ContainmentType.Disjoint);
                    bool bOccluded = _IsOccluded(i);

                    if (bIntersects)
                    {
                        List<PoseableNode> list = mNodes[i].Objects;
                        int count = list.Count;

                        for (int j = 0; j < count; j++)
                        {
                            PoseableNode entry = list[j];

                            if ((entry.ShadowMask & aLight.ShadowMask) != 0)
                            {
                                BoundingBox aabb = entry.AABB;

                                if (Utilities.Contains(Shared.ActiveShadowPlanes, ref aabb) != ContainmentType.Disjoint)
                                {
                                    bReturn = bReturn || entry.bMyShadowRequiresUpdate;

                                    if (aLight.bShadowsDirty) { entry.ShadowingPose(aLight); }

                                    if (!bOccluded && (entry.LightMask & aLight.LightMask) != 0)
                                    {
                                        if (Utilities.Contains(Shared.ActiveLightPlanes, ref aabb) != ContainmentType.Disjoint)
                                        {
                                            entry.LightingPose(aLight);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    i = _Next(bIntersects, i);
                }
            }
            else
            {
                for (int i = 0; i < mNodeCount; )
                {
                    bool bIntersects = (Utilities.Contains(Shared.ActiveLightPlanes, ref mNodes[i].AABB) != ContainmentType.Disjoint) && !_IsOccluded(i);

                    if (bIntersects)
                    {
                        List<PoseableNode> list = mNodes[i].Objects;
                        int count = list.Count;

                        for (int j = 0; j < count; j++)
                        {
                            PoseableNode entry = list[j];

                            if ((entry.LightMask & aLight.LightMask) != 0)
                            {
                                BoundingBox aabb = entry.AABB;

                                if (aLight.WorldBounding.Contains(aabb) != ContainmentType.Disjoint)
                                {
                                    if (Utilities.Contains(Shared.ActiveLightPlanes, ref aabb) != ContainmentType.Disjoint)
                                    {
                                        entry.LightingPose(aLight);
                                    }
                                }
                            }
                        }
                    }

                    i = _Next(bIntersects, i);
                }
            }

            return bReturn;
        }

        public void Pick(Cell aCell, ref Ray aWorldRay)
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = (Shared.ActiveWorldFrustum.Contains(ref mNodes[i].AABB) != ContainmentType.Disjoint);

                if (bIntersects)
                {
                    List<PoseableNode> list = mNodes[i].Objects;

                    int count = list.Count;
                    for (int j = 0; j < count; j++)
                    {
                        if (list[j].AABB.Intersects(aWorldRay) != null)
                        {
                            list[j].Pick(aCell, ref aWorldRay);
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }

        /// \todo Remove the near plane test and change occlusion query region rendering to use
        ///       a viewProjection transform that clamps the depth values against the near plane.
        public void Tick()
        {
#if DEBUG
            TotalQueriesIssued = 0;
#endif

            float factor = (mNodes[0].TotalFacesInSubtree * kMinimumFactorForOcclusionQuery);
            uint currentTick = Siat.Singleton.FrameTick;

            for (int i = 0; i < mNodeCount; i++)
            {
                if (factor < mNodes[i].TotalFacesInSubtree)
                {
                    if (mQueries[i].Query == null)
                    {
                        mQueries[i] = new OcclusionEntry(new OcclusionQuery(Siat.Singleton.GraphicsDevice));
                    }

                    Debug.Assert(mQueries[i].LastFrameTick != currentTick);

                    mQueries[i].LastFrameTick = currentTick;

                    PlaneIntersectionType intersection;
                    Shared.ActiveWorldFrustum.Planes[Frustum.kNear].Plane.Intersects(ref mNodes[i].AABB, out intersection);

                    if (intersection == PlaneIntersectionType.Intersecting)
                    {
                        mQueries[i].bUpdateOcclusion = true;
                        mQueries[i].bLastOcclusionCheck = false;
                    }
                    else
                    {
                        OcclusionQuery query = mQueries[i].Query;

                        if (query.IsComplete)
                        {
                            if (!mQueries[i].bUpdateOcclusion)
                            {
                                mQueries[i].bUpdateOcclusion = true;
                                mQueries[i].bLastOcclusionCheck = (query.PixelCount < kMinimumPixelCountForNotOccluded);
                            }
                        }

                        if (mQueries[i].bUpdateOcclusion)
                        {
                            if (Shared.ActiveWorldFrustum.Contains(ref mNodes[i].AABB) != ContainmentType.Disjoint)
                            {
                                RenderRoot.PoseOperations.OcclusionQuery(mWorldWrapped[i], query);
#if DEBUG
                                TotalQueriesIssued++;
#endif
                                mQueries[i].bUpdateOcclusion = false;
                            }
                        }
                    }
                }
                else if (mQueries[i].Query != null)
                {
                    mQueries[i].Query.Dispose();
                    mQueries[i].Query = null;
                }
            }
        }

        public void Update(PoseableNode aNode)
        {
            if (!aNode.mThis.IsValid)
            {
                Add(aNode);
            }
            else
            {
                Node[] nodes = aNode.mThis.mKdTree.mNodes;
                if (nodes[aNode.mThis.Index].AABB.Contains(aNode.AABB) != ContainmentType.Contains)
                {
                    aNode.mThis.Remove();
                    Add(aNode);
                }
            }
        }
    }
}
