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
    /// A general purpose Frustum. More flexible than the built-in XNA BoundingFrustum.
    /// </summary>
    /// <remarks>
    /// A Frustum object forms an enclosed region by planes like a standard 6-sided frustum, but it can
    /// have any number of lateral (side) planes. It is gauranteed to have at least
    /// 5 planes - the first two are expected to be parallel capping planes ("near"
    /// and "far" planes) and the last 3...n are lateral planes that may intersect, forming
    /// a point at Frustum.Center.
    /// 
    /// Frustum.Center is explicit to allow for orthogonal frusta whose lateral planes
    /// do not intersect at a common point.
    /// </remarks>
    public struct Frustum
    {
        public Vector3 Center;
        public readonly SiatPlane[] Planes;
        public const int kNear = 0;
        public const int kFar = 1;

        public const int kCappingPlaneCount = 2;
        public const int kMinimumLateralPlanes = 3;
        public const int kMinimumFrustumPlanes = kCappingPlaneCount + kMinimumLateralPlanes;

        public Frustum Clone()
        {
            Frustum ret = new Frustum(Center, Planes.Length);
            Planes.CopyTo(ret.Planes, 0);

            return ret;
        }

        public Frustum(Vector3 aCenter, int aCount)
        {
            Center = aCenter;
            int count = Utilities.Max(aCount, kMinimumFrustumPlanes);
            Planes = new SiatPlane[count];
        }

        public Frustum(Vector3 aCenter, ref Matrix aViewProjection)
        {
            Center = aCenter;
            Planes = new SiatPlane[6];
            Set(aCenter, ref aViewProjection);
        }

        public void Set(Vector3 aCenter, ref Matrix M)
        {
            Center = aCenter;

            // near
            Planes[kNear].Plane.Normal.X = M.M13;
            Planes[kNear].Plane.Normal.Y = M.M23;
            Planes[kNear].Plane.Normal.Z = M.M33;
            Planes[kNear].Plane.D = M.M43;
            Planes[kNear].Plane.Normalize();
            Planes[kNear].UpdateAbsNormal();

            // far
            Planes[kFar].Plane.Normal.X = M.M14 - M.M13;
            Planes[kFar].Plane.Normal.Y = M.M24 - M.M23;
            Planes[kFar].Plane.Normal.Z = M.M34 - M.M33;
            Planes[kFar].Plane.D = M.M44 - M.M43;
            Planes[kFar].Plane.Normalize();
            Planes[kFar].UpdateAbsNormal();

            // left
            Planes[2].Plane.Normal.X = M.M14 + M.M11;
            Planes[2].Plane.Normal.Y = M.M24 + M.M21;
            Planes[2].Plane.Normal.Z = M.M34 + M.M31;
            Planes[2].Plane.D = M.M44 + M.M41;
            Planes[2].Plane.Normalize();
            Planes[2].UpdateAbsNormal();

            // top
            Planes[3].Plane.Normal.X = M.M14 - M.M12;
            Planes[3].Plane.Normal.Y = M.M24 - M.M22;
            Planes[3].Plane.Normal.Z = M.M34 - M.M32;
            Planes[3].Plane.D = M.M44 - M.M42;
            Planes[3].Plane.Normalize();
            Planes[3].UpdateAbsNormal();

            // right
            Planes[4].Plane.Normal.X = M.M14 - M.M11;
            Planes[4].Plane.Normal.Y = M.M24 - M.M21;
            Planes[4].Plane.Normal.Z = M.M34 - M.M31;
            Planes[4].Plane.D = M.M44 - M.M41;
            Planes[4].Plane.Normalize();
            Planes[4].UpdateAbsNormal();

            // bottom
            Planes[5].Plane.Normal.X = M.M14 + M.M12;
            Planes[5].Plane.Normal.Y = M.M24 + M.M22;
            Planes[5].Plane.Normal.Z = M.M34 + M.M32;
            Planes[5].Plane.D = M.M44 + M.M42;
            Planes[5].Plane.Normalize();
            Planes[5].UpdateAbsNormal();
        }

        public ContainmentType Contains(ref BoundingBox aBox)
        {
            ContainmentType ret = ContainmentType.Contains;
            Vector3 rst = (aBox.Max - aBox.Min);
            Vector3 center = Utilities.GetCenter(ref aBox);
            
            int count = Planes.Length;
            for (int i = 0; i < count; i++)
            {
                float dot;
                Vector3.Dot(ref rst, ref Planes[i].AbsNormal, out dot);

                float radius = 0.5f * dot;
                float negRadius = -radius;

                float d;
                Planes[i].Plane.DotCoordinate(ref center, out d);

                if (d < negRadius) { return ContainmentType.Disjoint; }
                else if (d <= radius) { ret = ContainmentType.Intersects; }
            }

            return ret;
        }

        public ContainmentType Contains(ref BoundingSphere aSphere)
        {
            ContainmentType ret = ContainmentType.Contains;
            Vector3 center = aSphere.Center;
            float radius = aSphere.Radius;
            float negRadius = -radius;

            int count = Planes.Length;
            for (int i = 0; i < count; i++)
            {
                float d;
                Planes[i].Plane.DotCoordinate(ref center, out d);

                if (d < negRadius) { return ContainmentType.Disjoint; }
                else if (d <= radius) { ret = ContainmentType.Intersects; }
            }

            return ret;
        }

        public ContainmentType Contains(ref Vector3 aPoint)
        {
            ContainmentType ret = ContainmentType.Contains;

            int count = Planes.Length;

            for (int i = 0; i < count; i++)
            {
                float d;
                Planes[i].Plane.DotCoordinate(ref aPoint, out d);

                if (d < Utilities.kNegativeLooseToleranceFloat) { return ContainmentType.Disjoint; }
                else if (d <= Utilities.kLooseToleranceFloat) { ret = ContainmentType.Intersects; }
            }

            return ret;
        }
    }
}
