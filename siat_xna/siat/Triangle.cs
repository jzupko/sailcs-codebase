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

    public interface IConvex
    {
        Vector3 GetWorldSupport(Vector3 arNormal);
        Vector3 WorldTranslation { get; }
    }

    public struct Triangle : IkdTreeObject, IConvex
    {
        #region Overrides
        public Vector3 GetWorldSupport(Vector3 aNormal)
        {
            if (Vector3.Dot(P2 - P0, aNormal) > 0.0f)
            {
                if (Vector3.Dot(P2 - P1, aNormal) > 0.0f) { return P2; }
                else { return P1; }
            }
            else
            {
                if (Vector3.Dot(P1 - P0, aNormal) > 0.0f) { return P1; }
                else { return P0; }
            }
        }

        public Vector3 WorldTranslation
        {
            get
            {
                const float kFactor = (float)(1.0 / 3.0);
                return (kFactor * (P0 + P1 + P2));
            }
        }

        public BoundingBox AABB { get { return Box; } }
        public int FaceCount { get { return 1; } }
        #endregion

        public readonly BoundingBox Box;
        public readonly Plane Plane;

        public readonly Vector3 P0;
        public readonly Vector3 P1;
        public readonly Vector3 P2;

        public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;

            // Note: although XNA defaults to clockwise winding, the Plane constructor is setup like a counter-clockwise
            // wound environment. I.e. normal is calculated with Vector3.Normalize(Vector3.Cross(point2 - point1, point3 - point1)),
            // where point1, point2, point3 are arguments to the Plane() constructor.
            #region Plane calculation
#if SIAT_DEFAULT_CLOCKWISE_WINDING
            Plane = new Plane(P0, P2, P1);
#elif SIAT_DEFAULT_COUNTER_CLOCKWISE_WINDING
            Plane = new Plane(P0, P1, P2);
#endif
            #endregion

            #region AABB calculation
            Box.Min = Vector3.Min(P0, Vector3.Min(P1, P2));
            Box.Max = Vector3.Max(P0, Vector3.Max(P1, P2));

            Vector3 extents = Utilities.GetExtents(Box);
            if (extents.X < Utilities.kLooseToleranceFloat) { Box.Min.X -= Utilities.kLooseToleranceFloat; Box.Max.X += Utilities.kLooseToleranceFloat; }
            if (extents.Y < Utilities.kLooseToleranceFloat) { Box.Min.Y -= Utilities.kLooseToleranceFloat; Box.Max.Y += Utilities.kLooseToleranceFloat; }
            if (extents.Z < Utilities.kLooseToleranceFloat) { Box.Min.Z -= Utilities.kLooseToleranceFloat; Box.Max.Z += Utilities.kLooseToleranceFloat; }
            #endregion
        }

        public bool IsDegenerate
        {
            get
            {
                return (
                    Utilities.AboutEqual(P0, P1, Utilities.kLooseToleranceFloat) ||
                    Utilities.AboutEqual(P0, P2, Utilities.kLooseToleranceFloat) ||
                    Utilities.AboutEqual(P1, P2, Utilities.kLooseToleranceFloat));
            }
        }
    }
}
