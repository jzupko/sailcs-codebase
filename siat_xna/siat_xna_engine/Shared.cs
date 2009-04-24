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

namespace siat
{
    /// <summary>
    /// Common shared data across the engine.
    /// </summary>
    /// <remarks>
    /// It is expected that everything "plays nice" when dealing with this class. Objects that should
    /// be setting values set them and objects that should only be getting values only get them.
    /// </remarks>
    public static class Shared
    {
        public const float kInfiniteProjEpsilon = 2.4e-7f - 1.0f;

        #region Private members
        private static MatrixWrapper mInfiniteProjectionTransform = new MatrixWrapper();
        private static MatrixWrapper mInfiniteViewProjectionTransform = new MatrixWrapper();
        private static MatrixWrapper mInverseProjectionTransform = new MatrixWrapper();
        private static MatrixWrapper mInverseViewTransform = new MatrixWrapper();
        private static MatrixWrapper mInverseViewProjectionTransform = new MatrixWrapper();
        private static MatrixWrapper mProjectionTransform = new MatrixWrapper();
        private static MatrixWrapper mViewTransform = new MatrixWrapper();
        private static MatrixWrapper mViewProjectionTransform = new MatrixWrapper();

        private static Vector3 mViewDown = Vector3.Down;
        private static Vector3 mViewLeft = Vector3.Left;
        private static Vector3 mViewRight = Vector3.Right;
        private static Vector3 mViewUp = Vector3.Up;
        private static Frustum mWorldFrustum = new Frustum(Vector3.Zero, 6);
        #endregion;

        public static Matrix ProjectionTransform
        {
            get
            {
                return mProjectionTransform.Matrix;
            }

            set
            {
                mProjectionTransform.Matrix = value;
                mInverseProjectionTransform.Matrix = Matrix.Invert(mProjectionTransform.Matrix);

                #region Calculate infinite projection transform
                // see http://www.terathon.com/gdc07_lengyel.ppt
                float near = mProjectionTransform.Matrix.M43 / mProjectionTransform.Matrix.M33;

                mInfiniteProjectionTransform.Matrix =
                    new Matrix(mProjectionTransform.Matrix.M11, 0, 0, 0,
                               0, mProjectionTransform.Matrix.M22, 0, 0,
                               mProjectionTransform.Matrix.M31, mProjectionTransform.Matrix.M32, kInfiniteProjEpsilon, -1.0f,
                               0, 0, kInfiniteProjEpsilon * near, 0);
                #endregion

                mInfiniteViewProjectionTransform.Matrix = mViewTransform.Matrix * mInfiniteProjectionTransform.Matrix;
                mViewProjectionTransform.Matrix = mViewTransform.Matrix * mProjectionTransform.Matrix;
                mInverseViewProjectionTransform.Matrix = Matrix.Invert(mViewProjectionTransform.Matrix);
                mWorldFrustum.Set(mInverseViewTransform.Matrix.Translation, ref mViewProjectionTransform.Matrix);
            }
        }

        public static Matrix ViewTransform
        {
            get
            {
                return mViewTransform.Matrix;
            }

            set
            {
                mViewTransform.Matrix = value;
                mViewDown = Vector3.TransformNormal(Vector3.Down, mViewTransform.Matrix);
                mViewLeft = Vector3.TransformNormal(Vector3.Left, mViewTransform.Matrix);
                mViewRight = Vector3.TransformNormal(Vector3.Right, mViewTransform.Matrix);
                mViewUp = Vector3.TransformNormal(Vector3.Up, mViewTransform.Matrix);

                mInverseViewTransform.Matrix = Matrix.Invert(mViewTransform.Matrix);
                mInfiniteViewProjectionTransform.Matrix = mViewTransform.Matrix * mInfiniteProjectionTransform.Matrix;
                mViewProjectionTransform.Matrix = mViewTransform.Matrix * mProjectionTransform.Matrix;
                mInverseViewProjectionTransform.Matrix = Matrix.Invert(mViewProjectionTransform.Matrix);
                mWorldFrustum.Set(mInverseViewTransform.Matrix.Translation, ref mViewProjectionTransform.Matrix);
            }
        }

        public static Matrix InfiniteProjectionTransform { get { return mInfiniteProjectionTransform.Matrix; } }
        public static Matrix InfiniteViewProjectionTransform { get { return mInfiniteViewProjectionTransform.Matrix; } }
        public static Matrix InverseProjectionTransform { get { return mInverseProjectionTransform.Matrix; } }
        public static Matrix InverseViewTransform { get { return mInverseViewTransform.Matrix; } }
        public static Matrix InverseViewProjectionTransform { get { return mInverseViewProjectionTransform.Matrix; } }
        public static Matrix ViewProjectionTransform { get { return mViewProjectionTransform.Matrix; } }

        public static MatrixWrapper InfiniteProjectionTransformWrapped { get { return mInfiniteProjectionTransform; } }
        public static MatrixWrapper InfiniteViewProjectionTransformWrapped { get { return mInfiniteViewProjectionTransform; } }
        public static MatrixWrapper InverseProjectionTransformWrapped { get { return mInverseProjectionTransform; } }
        public static MatrixWrapper InverseViewTransformWrapped { get { return mInverseViewTransform; } }
        public static MatrixWrapper InverseViewProjectionTransformWrapped { get { return mInverseViewProjectionTransform; } }
        public static MatrixWrapper ProjectionTransformWrapped { get { return mProjectionTransform; } }
        public static MatrixWrapper ViewTransformWrapped { get { return mViewTransform; } }
        public static MatrixWrapper ViewProjectionTransformWrapped { get { return mViewProjectionTransform; } }

        public static Frustum ActiveWorldFrustum { get { return mWorldFrustum; } set { mWorldFrustum = value; } }

        public static Vector3 ViewDown { get { return mViewDown; } }
        public static Vector3 ViewLeft { get { return mViewLeft; } }
        public static Vector3 ViewRight { get { return mViewRight; } }
        public static Vector3 ViewUp { get { return mViewUp; } }

        public static SiatPlane[] ActiveLightPlanes;
        public static SiatPlane[] ActiveShadowPlanes;
    }
}
