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

namespace siat
{
    /// <summary>
    /// Defines an oriented bounding box (OBB.
    /// </summary>
    /// <remarks>
    /// An OBB has is a box with arbitrary orientation. It is defined by three perpendicular axes,
    /// a center position, and three extents (one each along each axis). This is in contrast to
    /// an axis-aligned bounding box (AABB) which is always aligned to the XYZ coordinate frame and
    /// can therefore be defined by a minimum vector and a maximum vector.
    /// </remarks>
    /// 
    /// \sa Microsoft.Xna.Framework.BoundingBox
    public struct OrientedBoundingBox
    {
        public Vector3 Center;
        public Vector3 Rmag;
        public Vector3 Smag;
        public Vector3 Tmag;

        public OrientedBoundingBox(ref Vector3 aCenter, ref Vector3 aHalfExtents,
            ref Vector3 aR, ref Vector3 aS, ref Vector3 aT)
        {
            Center = aCenter;
            Rmag = aR * aHalfExtents.X;
            Smag = aS * aHalfExtents.Y;
            Tmag = aT * aHalfExtents.Z;
        }

        public static OrientedBoundingBox CreateFromPoints(IEnumerable<Vector3> aPoints)
        {
            Vector3 center;
            Vector3 r;
            Vector3 s;
            Vector3 t;
            Vector3 halfExtents;

            Utilities.CalculatePrincipalComponentAxes(aPoints, out r, out s, out t);
            Utilities.CalculateCenterAndHalfExtents(aPoints, ref r, ref s, ref t, out center, out halfExtents);

            OrientedBoundingBox ret = new OrientedBoundingBox(ref center, ref halfExtents, ref r, ref s, ref t);

            return ret;
        }
        
        public bool Intersects(BoundingBox aAABB)
        {
            Vector3 aabbCenter = Utilities.GetCenter(aAABB);
            Vector3 aabbHalfExtents = Utilities.GetHalfExtents(aAABB);

            Vector3 diff = (aabbCenter - Center);

            #region X axis
            {
                float d = Math.Abs(Rmag.X) + Math.Abs(Smag.X) + Math.Abs(Tmag.X);
                float r = Math.Abs(diff.X);
                if ((r - d) > aabbHalfExtents.X) { return false; }
            }
            #endregion

            #region Y axis
            {
                float d = Math.Abs(Rmag.Y) + Math.Abs(Smag.Y) + Math.Abs(Tmag.Y);
                float r = Math.Abs(diff.Y);
                if ((r - d) > aabbHalfExtents.Y) { return false; }

            }
            #endregion

            #region Z axis
            {
                float d = Math.Abs(Rmag.Z) + Math.Abs(Smag.Z) + Math.Abs(Tmag.Z);
                float r = Math.Abs(diff.Z);
                if ((r - d) > aabbHalfExtents.Z) { return false; }
            }
            #endregion

            return true;
        }

        public PlaneIntersectionType Intersects(Plane aPlane)
        {
            float radius = Utilities.EffectiveRadius(ref this, ref aPlane);
            float negRadius = -radius;

            float d;
            aPlane.DotCoordinate(ref Center, out d);

            if (d < negRadius) { return PlaneIntersectionType.Back; }
            else if (d <= radius) { return PlaneIntersectionType.Intersecting; }
            else { return PlaneIntersectionType.Front; }
        }

        public bool Intersects(Vector3 aPoint)
        {
            Vector3 diff = (aPoint - Center);
            float adr = Math.Abs(Vector3.Dot(diff, Rmag));
            float ads = Math.Abs(Vector3.Dot(diff, Smag));
            float adt = Math.Abs(Vector3.Dot(diff, Tmag));

            if (adr < 0.0f && ads < 0.0f && adt < 0.0f) { return true; }
            else { return false; }
        }

        public override string ToString()
        {
            return "Center: " + Center.ToString() +
                   " Rmag: " + Rmag.ToString() +
                   " Smag: " + Smag.ToString() +
                   " Tmag: " + Tmag.ToString();
        }
    }
}
