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
    /// A rotation-translation (RT) transform.
    /// </summary>
    /// <remarks>
    /// Although this class uses a generic 3x3 transform to store orientation, it
    /// assumes that it is an orthonormal rotation matrix and that (N^-1) = (N^T)
    /// </remarks>
    public struct CoordinateFrame
    {
        public Matrix3 Orientation;
        public Vector3 Translation;

        public CoordinateFrame(Matrix3 aOrientation, Vector3 aTranslation)
        {
            Orientation = aOrientation;
            Translation = aTranslation;
        }

        public static CoordinateFrame operator *(CoordinateFrame a, CoordinateFrame b)
        {
            CoordinateFrame ret;

            ret.Orientation = (a.Orientation * b.Orientation);
            ret.Translation = b.Orientation.Transform(a.Translation) + b.Translation;

            return ret;
        }

        public static CoordinateFrame Identity
        {
            get
            {
                CoordinateFrame ret;
                ret.Orientation = Matrix3.Identity;
                ret.Translation = Vector3.Zero;

                return ret;
            }
        }

        public static CoordinateFrame Invert(CoordinateFrame aFrame)
        {
            CoordinateFrame ret;

            Matrix3.Transpose(ref aFrame.Orientation, out ret.Orientation);
            ret.Translation = ret.Orientation.Transform(-aFrame.Translation);

            return ret;
        }

        public static CoordinateFrame Lerp(ref CoordinateFrame a, ref CoordinateFrame b, float aWeightOfB)
        {
            CoordinateFrame ret;
            ret.Orientation = Matrix3.Lerp(ref a.Orientation, ref b.Orientation, aWeightOfB);
            ret.Translation = Vector3.Lerp(a.Translation, b.Translation, aWeightOfB);

            return ret;
        }

        public Matrix ToMatrix()
        {
            Matrix ret = Orientation.ToMatrix();
            ret.Translation = Translation;

            return ret;
        }

        public override string ToString()
        {
            return "Orientation: " + Orientation.ToString() + " , Translation: " + Translation.ToString();
        }

        private static Vector3[] mCornerBuffer = new Vector3[8];

        public static void Transform(ref BoundingBox aAABB, ref CoordinateFrame c, out BoundingBox arAABB)
        {
            aAABB.GetCorners(mCornerBuffer);

            for (int i = 0; i < 8; i++)
            {
                Transform(ref mCornerBuffer[i], ref c, out mCornerBuffer[i]);
            }

            arAABB = BoundingBox.CreateFromPoints(mCornerBuffer);
        }

        public static void Transform(ref Vector3 u, ref CoordinateFrame c, out Vector3 v)
        {
            Matrix3.Transform(ref u, ref c.Orientation, out v);
            v += c.Translation;
        }

        public Vector3 Transform(Vector3 v)
        {
            Vector3 ret;
            Transform(ref v, ref (this), out ret);

            return ret;
        }

        public static void TransformNormal(ref Vector3 u, ref CoordinateFrame c, out Vector3 v)
        {
            Matrix3.Transform(ref u, ref c.Orientation, out v);
        }

        public Vector3 TransformNormal(Vector3 v)
        {
            Vector3 ret;
            Matrix3.Transform(ref v, ref Orientation, out ret);

            return ret;
        }
    }
}
