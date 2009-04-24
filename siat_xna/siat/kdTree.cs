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

namespace siat
{
    /// <summary>
    /// An object containable in the kdTree.
    /// </summary>
    public interface IkdTreeObject
    {
        /// <summary>
        /// The AABB of this object, expected to be in the same coordinate frame as the kdTree.
        /// </summary>
        BoundingBox AABB { get; }

        /// <summary>
        /// Total face count for this object.
        /// </summary>
        int FaceCount { get; }
    }

    public struct kdTreeCoefficients
    {
        public kdTreeCoefficients(float aIntersection, float aLocalization, float aSplit)
        {
            Intersection = aIntersection;
            Localization = aLocalization;
            Split = aSplit;
        }

        /// <summary>
        /// Importance of reducing the surface area and primitive count in a node.
        /// </summary>
        public float Intersection;

        /// <summary>
        /// Importance of splitting the longest axis.
        /// </summary>
        public float Localization;

        /// <summary>
        /// Importance of reducing the number of objects intersecting a splitting plane.
        /// </summary>
        public float Split;
    }

    /// <summary>
    /// A kdTree.
    /// </summary>
    /// <remarks>
    /// A space-partitioning data structure: (http://en.wikipedia.org/wiki/Kd-tree).
    /// 
    /// This is a stackless kdTree with all nodes tightly packed at the beginning of the array.
    /// As a result, all nodes [0, mNodeCount) are valid kdTree nodes and operations over
    /// the entire tree can be accomplished by iterating over this range.
    /// 
    /// Splitting heuristic based on the surface area heuristic (SAH): 
    ///     Havran, V. 2000. "Heuristic Ray Shooting Algorithms", Ph.D. Thesis, 
    ///     Department of Computer Science and Engineering, Czech Technical University in Prague,
    ///     http://www.cgg.cvut.cz/~havran/phdthesis.html .
    /// 
    /// With some ideas taken from:
    ///     Fleisz, M. 2006. "Spatial Partitioning Using an Adaptive Binary Tree", 
    ///     Game Programming Gems 6, 423, ISBN: 1-58450-450-1
    /// </remarks>    /// 
    public abstract class kdTree<T>
        where T : IkdTreeObject
    {
        public const int kMinimumDepth = 0;
        public const int kMaximumDepth = 10;

        public const int kMinimumSplitCount = 2;

        #region Protected members
        protected struct BuildHelper : IEquatable<T>
        {
            BuildHelper(T aObject, bool abMin)
            {
                Object = aObject;
                bMin = abMin;
            }

            public T Object;
            public bool bMin;

            public bool Equals(T b)
            {
                return (Object.Equals(b));
            }
        }

        protected readonly static Comparison<BuildHelper>[] msComparer = new Comparison<BuildHelper>[3]
            {   
                delegate(BuildHelper a, BuildHelper b)
                {
                    float c0 = (a.bMin) ? a.Object.AABB.Min.X : a.Object.AABB.Max.X;
                    float c1 = (b.bMin) ? b.Object.AABB.Min.X : b.Object.AABB.Max.X;

                    int c = c0.CompareTo(c1);

                    if (c == 0) { return (a.bMin && b.bMin) ? 0 : ((a.bMin) ? -1 : 1); }
                    else { return c; }
                },

                delegate(BuildHelper a, BuildHelper b)
                {
                    float c0 = (a.bMin) ? a.Object.AABB.Min.Y : a.Object.AABB.Max.Y;
                    float c1 = (b.bMin) ? b.Object.AABB.Min.Y : b.Object.AABB.Max.Y;

                    int c = c0.CompareTo(c1);

                    if (c == 0) { return (a.bMin && b.bMin) ? 0 : ((a.bMin) ? -1 : 1); }
                    else { return c; }
                },

                delegate(BuildHelper a, BuildHelper b)
                {
                    float c0 = (a.bMin) ? a.Object.AABB.Min.Z : a.Object.AABB.Max.Z;
                    float c1 = (b.bMin) ? b.Object.AABB.Min.Z : b.Object.AABB.Max.Z;

                    int c = c0.CompareTo(c1);

                    if (c == 0) { return (a.bMin && b.bMin) ? 0 : ((a.bMin) ? -1 : 1); }
                    else { return c; }
                }
            };

        protected void _Allocate()
        {
            mNodes = new Node[(1 << mDepth)];
        }

        protected void _Compact()
        {
            Array.Resize(ref mNodes, mNodeCount);
        }

        protected float _CalculateCost(float aParentInvSA, ref BoundingBox aBackAABB, ref BoundingBox aFrontAABB,
            int aBackFaceCount, int aFrontFaceCount, int aSplitCount, Axis aAxis)
        {
            float backSA = Utilities.GetSurfaceArea(ref aBackAABB);
            float frontSA = Utilities.GetSurfaceArea(ref aFrontAABB);

            float axisLength = Utilities.GetElement(aFrontAABB.Max, (int)aAxis) - Utilities.GetElement(aBackAABB.Min, (int)aAxis);
            float axisCost = (Utilities.GreaterThan(axisLength, 0.0f)) ? 1.0f / axisLength : 1.0f;

            float cost = ((mCoeff.Localization * axisCost) +
                          (mCoeff.Split * aSplitCount) +
                          (mCoeff.Intersection * aParentInvSA * ((backSA * aBackFaceCount) + (frontSA * aFrontFaceCount))));

            return cost;
        }

        protected void _Split(List<T> aObjects, int aDepth, int aParentIndex, int aParentFaceCount)
        {
            // Early out, generates a leaf node.
            if (aDepth >= mDepth || aObjects.Count < kMinimumSplitCount)
            {
                mNodes[aParentIndex].Sibling = mNodeCount;
                return;
            }

            // Generate a (sort of) SAP structure (sweep and prune). Insert the min/max vectors
            // for each bounding box of each object, then sort by the current axis. This is an 
            // efficient way of determining how many boxes we are within as we step through the
            // list.
            int count = aObjects.Count;
            BuildHelper[] sap = new BuildHelper[count * 2];

            for (int i = 0; i < count; i++)
            {
                int index = i * 2;

                sap[index + 0].bMin = true;
                sap[index + 0].Object = aObjects[i];

                sap[index + 1].bMin = false;
                sap[index + 1].Object = aObjects[i];
            }

            // Modified version of summed-area heuristic (SAH) - added a term that weights
            // the split count for a position - basically, how many faces will get bubbled
            // up towards the root of the tree based on a split position (each AABB must
            // be completely contained in a kdTree node, so if it intersects, it bubbles up
            // to the parent). Also check all axes and weight instead of just choosing the
            // largest axis.
            Axis bestAxis = Axis.X;
            float bestCost = float.MaxValue;
            float bestPosition = 0.0f;
            BoundingBox bestBack = mNodes[aParentIndex].AABB;
            BoundingBox bestFront = bestBack;
            int bestBackFaceCount = 0;
            int bestFrontFaceCount = aParentFaceCount;
            
            float parentInvSA = Utilities.GetInverseSurfaceArea(ref mNodes[aParentIndex].AABB);
            float parentCost = (mCoeff.Intersection * aParentFaceCount);

            count = sap.Length;

            for (int j = 0; j < (int)Axis.W; j++)
            {
                Axis axis = (Axis)j;
                int axisIndex = j;
                Array.Sort(sap, msComparer[axisIndex]);
                int backFaceCount = 0;
                BoundingBox backAABB = mNodes[aParentIndex].AABB;
                int frontFaceCount = aParentFaceCount;
                BoundingBox frontAABB = backAABB;
                int splitCount = 0;

                #region Find the best split position
                for (int i = 0; i < count; i++)
                {
                    int faceCount = sap[i].Object.FaceCount;

                    // If this is not a minimum edge, then we are no longer splitting this object and the faces
                    // can be added to the left face count.
                    if (!sap[i].bMin)
                    {
                        splitCount -= faceCount;
                        splitCount = Utilities.Clamp(splitCount, 0, aParentFaceCount);
                        backFaceCount += faceCount;
                        backFaceCount = Utilities.Clamp(backFaceCount, 0, aParentFaceCount);
                    }

                    float f = Utilities.GetElement((sap[i].bMin) ? sap[i].Object.AABB.Min : sap[i].Object.AABB.Max, axisIndex);
                    f = (sap[i].bMin) ? f - Utilities.kLooseToleranceFloat : f + Utilities.kLooseToleranceFloat;

                    Utilities.SetElement(ref backAABB.Max, axisIndex, f);
                    Utilities.SetElement(ref frontAABB.Min, axisIndex, f);

                    float cost = _CalculateCost(parentInvSA, ref backAABB, ref frontAABB, backFaceCount, frontFaceCount, splitCount, axis);

                    if (cost < bestCost)
                    {
                        bestAxis = axis;
                        bestCost = cost;
                        bestPosition = f;
                        bestBack = backAABB;
                        bestFront = frontAABB;
                        bestBackFaceCount = backFaceCount;
                        bestFrontFaceCount = frontFaceCount;
                    }

                    // If this is a minimum edge, then in the next iteration, we'll be splitting this box and
                    // the number of faces to the right is reduced by this objects face count (since they'll
                    // either end up to the left of the split or in the parent).
                    if (sap[i].bMin)
                    {
                        splitCount += faceCount;
                        frontFaceCount -= faceCount;
                        frontFaceCount = Utilities.Clamp(frontFaceCount, 0, aParentFaceCount);
                    }
                }
                #endregion
            }

            if (bestCost < parentCost)
            {
                List<T> backObjects = new List<T>();
                List<T> frontObjects = new List<T>();

                Plane splitter = new Plane(Utilities.GetVectorFromAxis(bestAxis), -bestPosition);

                Split(aObjects, ref splitter, backObjects, frontObjects);

                int back = mNodeCount++;
                mNodes[back] = new Node(ref bestBack);
                mNodes[back].Objects = backObjects;
                mNodes[back].Parent = aParentIndex;
                mNodes[back].TotalFacesInSubtree = bestBackFaceCount;
                _Split(mNodes[back].Objects, aDepth + 1, back, bestBackFaceCount);

                int front = mNodeCount++;
                mNodes[front] = new Node(ref bestFront);
                mNodes[front].Objects = frontObjects;
                mNodes[front].Parent = aParentIndex;
                mNodes[front].TotalFacesInSubtree = bestFrontFaceCount;
                _Split(mNodes[front].Objects, aDepth + 1, front, bestFrontFaceCount);
            }

            mNodes[aParentIndex].Sibling = mNodeCount;
        }

        protected bool _IsNull(int i) { return (i < 0); }
        protected bool _IsLeaf(int i) { return (mNodes[i].Sibling == (i + 1)); }
        protected int _Next(bool bIntersects, int i) { return (bIntersects) ? i + 1 : mNodes[i].Sibling; }

        protected kdTreeCoefficients mCoeff;
        protected int mDepth = 0;
        protected Node[] mNodes = null;
        protected int mNodeCount = 0;

        protected void _Build(IList<T> aObjects)
        {
            List<T> objects = new List<T>(aObjects);
            int faceCount = GetFaceCount(objects);

            mNodeCount = 0;

            _Allocate();
            mNodes[0].AABB = MergeAABB(objects);
            mNodes[0].Objects = objects;
            mNodes[0].Parent = -1;
            mNodes[0].Sibling = -1;
            mNodes[0].TotalFacesInSubtree = faceCount;

            mNodeCount++;
            _Split(objects, 1, 0, faceCount);
            _Compact();
        }
        #endregion

        #region Node
        /// <summary>
        /// A single node in the kdTree.
        /// </summary>
        public struct Node
        {
            public Node(ref BoundingBox aAABB)
            {
                AABB = aAABB;
                Objects = null;
                Parent = -1;
                Sibling = -1;
                TotalFacesInSubtree = 0;
            }

            public BoundingBox AABB;
            public List<T> Objects;
            public int Parent;
            public int Sibling;
            public int TotalFacesInSubtree;
        }
        #endregion

        #region Static public members
        /// <summary>
        /// Returns a Coefficients struct populated with default coefficients.
        /// </summary>
        public static kdTreeCoefficients DefaultCoefficients
        {
            get
            {
                kdTreeCoefficients ret;
                ret.Intersection = 1.0f;
                ret.Localization = 1.0f;
                ret.Split = 1.0f;

                return ret;
            }
        }

        /// <summary>
        /// Calculates the total faces in a collection of T objects.
        /// </summary>
        /// <param name="aObjects">The collection of objects.</param>
        /// <returns>The total face count.</returns>
        public static int GetFaceCount(IList<T> aObjects)
        {
            int ret = 0;

            int count = aObjects.Count;
            for (int i = 0; i < count; i++) { ret += aObjects[i].FaceCount; }

            return ret;
        }

        /// <summary>
        /// A brute-force enclosing AABB calculation for a container of T objects.
        /// </summary>
        /// <param name="aObjects">A container of objects.</param>
        /// <returns>The resulting AABB.</returns>
        public static BoundingBox MergeAABB(IList<T> aObjects)
        {
            BoundingBox ret = Utilities.kInvertedMaxBox;

            int count = aObjects.Count;
            for (int i = 0; i < count; i++)
            {
                ret = BoundingBox.CreateMerged(ret, aObjects[i].AABB);
            }

            return ret;
        }

        /// <summary>
        /// Calculates the next axis to be used when building a kdTree. Chooses the axis with
        /// the greatest length based on an AABB.
        /// </summary>
        /// <param name="aAABB">The AABB of a set of points.</param>
        /// <returns>The next axis to use for kdTree split.</returns>
        public static Axis NextAxis(ref BoundingBox aAABB)
        {
            Vector3 v = Utilities.GetHalfExtents(ref aAABB);

            return Utilities.GetMaxAxis(v);
        }

        /// <summary>
        /// Splits a collection of objects into two collections based on a splitting plane.
        /// </summary>
        /// <param name="aIn">Input collection.</param>
        /// <param name="aPlane">The splitting plane.</param>
        /// <param name="aBack">Objects completely on the back of the plane.</param>
        /// <param name="aFront">Objects completely on the front of the plane.</param>
        public static void Split(List<T> aIn, ref Plane aPlane, List<T> aBack, List<T> aFront)
        {
            int count = aIn.Count;
            for (int i = 0; i < count; i++)
            {
                PlaneIntersectionType test = aPlane.Intersects(aIn[i].AABB);

                if (test == PlaneIntersectionType.Back)
                {
                    T obj = aIn[i];
                    aIn[i] = aIn[count - 1]; 
                    count--; i--;
                    aBack.Add(obj);
                }
                else if (test == PlaneIntersectionType.Front)
                {
                    T obj = aIn[i];
                    aIn[i] = aIn[count - 1];
                    count--; i--;
                    aFront.Add(obj);
                }
            }

            aIn.RemoveRange(count, (aIn.Count - count));
        }
        #endregion

        public kdTree(kdTreeCoefficients aCoeff) : this(aCoeff, kMinimumDepth) { }
        public kdTree(kdTreeCoefficients aCoeff, int aDepth)
        {
            // Allocates space for one node.
            _Allocate();
            mCoeff = aCoeff;
            mDepth = Utilities.Clamp(aDepth, kMinimumDepth, kMaximumDepth);

            mNodes[0] = new Node(ref Utilities.kMaxBox);
            mNodes[0].Objects = new List<T>();
            mNodeCount = 1;
            mNodes[0].Parent = -1;
            mNodes[0].Sibling = mNodeCount;
        }

        public int RootFaceCount { get { return mNodes[0].TotalFacesInSubtree; } }
        public BoundingBox RootAABB { get { return mNodes[0].AABB; } }

        public void Query<U>(ref BoundingBox aAABB, List<U> aIntersections)
            where U : T
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = mNodes[i].AABB.Contains(aAABB) != ContainmentType.Disjoint;

                if (bIntersects)
                {
                    ContainmentType result;

                    List<T> list = mNodes[i].Objects;
                    int count = list.Count;

                    for (int j = 0; j < count; j++)
                    {
                        T entry = list[j];

                        if (entry is U)
                        {
                            entry.AABB.Contains(ref aAABB, out result);

                            if (result != ContainmentType.Disjoint)
                            {
                                aIntersections.Add((U)entry);
                            }
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }

        public void Query<U>(ref BoundingSphere aSphere, List<U> aIntersections)
            where U : T
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = mNodes[i].AABB.Contains(aSphere) != ContainmentType.Disjoint;

                if (bIntersects)
                {
                    ContainmentType result;

                    List<T> list = mNodes[i].Objects;
                    int count = list.Count;

                    for (int j = 0; j < count; j++)
                    {
                        T entry = list[j];

                        if (entry is U)
                        {
                            entry.AABB.Contains(ref aSphere, out result);

                            if (result != ContainmentType.Disjoint)
                            {
                                aIntersections.Add((U)entry);
                            }
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }

        public void Query<U>(ref Ray aRay, List<U> aIntersections)
            where U : T
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = (aRay.Intersects(mNodes[i].AABB) != null);

                if (bIntersects)
                {
                    List<T> list = mNodes[i].Objects;
                    int count = list.Count;

                    for (int j = 0; j < count; j++)
                    {
                        T entry = list[j];

                        if (entry is U)
                        {
                            bool b = (aRay.Intersects(entry.AABB) != null);

                            if (b)
                            {
                                aIntersections.Add((U)entry);
                            }
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }

        public void Query<U>(ref Vector3 aPoint, List<U> aIntersections)
            where U : T
        {
            for (int i = 0; i < mNodeCount; )
            {
                bool bIntersects = mNodes[i].AABB.Contains(aPoint) != ContainmentType.Disjoint;

                if (bIntersects)
                {
                    ContainmentType result;

                    List<T> list = mNodes[i].Objects;
                    int count = list.Count;

                    for (int j = 0; j < count; j++)
                    {
                        T entry = list[j];

                        if (entry is U)
                        {
                            entry.AABB.Contains(ref aPoint, out result);

                            if (result != ContainmentType.Disjoint)
                            {
                                aIntersections.Add((U)entry);
                            }
                        }
                    }
                }

                i = _Next(bIntersects, i);
            }
        }
    }

}
