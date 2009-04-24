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
using System.Runtime.InteropServices;

namespace siat
{
    /// <summary>
    /// Basic 3x3 Matrix.
    /// </summary>
    /// <remarks>
    /// XNA does not include a 3x3 Matrix type and this is useful in some cases, particularly
    /// to avoid unnecessary calculations (see Utilities.IsOrthogonal).
    /// </remarks>
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
    public struct Matrix3 : IEquatable<Matrix3>
    {
        public const int S = 3;
        public const int N = S * S;

        [FieldOffset(0)]
        unsafe fixed float Data[N];

        [FieldOffset(0)]
        public float M11;
        [FieldOffset(4)]
        public float M12;
        [FieldOffset(8)]
        public float M13;
        [FieldOffset(12)]
        public float M21;
        [FieldOffset(16)]
        public float M22;
        [FieldOffset(20)]
        public float M23;
        [FieldOffset(24)]
        public float M31;
        [FieldOffset(28)]
        public float M32;
        [FieldOffset(32)]
        public float M33;

        public Matrix3(float m11, float m12, float m13,
                       float m21, float m22, float m23,
                       float m31, float m32, float m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        public static bool operator ==(Matrix3 a, Matrix3 b)
        {
            return (a.M11 == b.M11) && (a.M12 == b.M12) && (a.M13 == b.M13) &&
                   (a.M21 == b.M21) && (a.M22 == b.M22) && (a.M23 == b.M23) &&
                   (a.M31 == b.M31) && (a.M32 == b.M32) && (a.M33 == b.M33);
        }

        public static bool operator !=(Matrix3 a, Matrix3 b)
        {
            return !(a == b);
        }

        public static Matrix3 operator *(float s, Matrix3 a)
        {
            return new Matrix3(a.M11 * s, a.M12 * s, a.M13 * s,
                               a.M21 * s, a.M22 * s, a.M23 * s,
                               a.M31 * s, a.M32 * s, a.M33 * s);
        }

        public static Matrix3 operator *(Matrix3 a, float s)
        {
            return new Matrix3(a.M11 * s, a.M12 * s, a.M13 * s,
                               a.M21 * s, a.M22 * s, a.M23 * s,
                               a.M31 * s, a.M32 * s, a.M33 * s);
        }

        public static Matrix3 operator*(Matrix3 a, Matrix3 b)
        {
            return new Matrix3(a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
                               a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
                               a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33); 
        }

        public static Matrix3 operator -(Matrix3 a, Matrix3 b)
        {
            return new Matrix3(a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13,
                               a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23,
                               a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33);
        }

        public static Matrix3 operator +(Matrix3 a, Matrix3 b)
        {
            return new Matrix3(a.M11 + b.M11, a.M12 + b.M12, a.M13 + b.M13,
                               a.M21 + b.M21, a.M22 + b.M22, a.M23 + b.M23,
                               a.M31 + b.M31, a.M32 + b.M32, a.M33 + b.M33);
        }

        public bool Equals(Matrix3 m)
        {
            return (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
                   (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) &&
                   (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
        }

        public override bool Equals(object obj)
        {
            if (obj is Matrix3)
            {
                Matrix3 m = (Matrix3)obj;

                return (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
                       (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) &&
                       (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
            }

            return false;
        }

        public static Matrix3 CreateFromUpperLeft(ref Matrix m)
        {
            Matrix3 ret = new Matrix3(m.M11, m.M12, m.M13,
                                      m.M21, m.M22, m.M23,
                                      m.M31, m.M32, m.M33);

            return ret;
        }

        public float this[int i, int j]
        {
            get
            {
                unsafe
                {
                    fixed (float* p = Data)
                    {
                        return p[(i * S) + j];
                    }
                }
            }

            set
            {
                unsafe
                {
                    fixed (float* p = Data)
                    {
                        p[(i * S) + j] = value;
                    }
                }
            }
        }

        public float GetDeterminant()
        {
            float ret = ((M11 * M22 * M33)  +
                         (M12 * M23 * M31)  + 
                         (M13 * M21 * M32)) -
    
                        ((M31 * M22 * M13) + 
                         (M32 * M23 * M11) + 
                         (M33 * M21 * M12));

            return ret;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static readonly Matrix3 Identity = new Matrix3(1, 0, 0,
                                                      0, 1, 0,
                                                      0, 0, 1);

        public static readonly Matrix3 Zero = new Matrix3(0, 0, 0,
                                                          0, 0, 0,
                                                          0, 0, 0);

        public static Matrix3 Invert(Matrix3 m)
        {
            Matrix3 ret;
            Invert(ref m, out ret);

            return ret;
        }

        /// <summary>
        /// Inversion of Matrix3 by parts.
        /// </summary>
        /// <param name="m">The Matrix3 to invert.</param>
        /// <param name="n">The resulting inverted Matrix.</param>
        public static void Invert(ref Matrix3 m, out Matrix3 n)
        {
            float n11 = m.M11; float n12 = m.M12; float n13 = m.M13;
            float n21 = m.M21; float n22 = m.M22; float n23 = m.M23;
            float n31 = m.M31; float n32 = m.M32; float n33 = m.M33;

            // entries in new matrix p to be multiplied by (1 / |M|)
            float p11 = (n22 * n33) - (n23 * n32);
            float p21 = (n23 * n31) - (n21 * n33);
            float p31 = (n21 * n32) - (n22 * n31);

            // 1 / |M|
            float t = 1.0f / ((n11 * p11) + (n12 * p21) + (n13 * p31));

            n.M11 = p11 * t; n.M12 = ((n13 * n32) - (n12 * n33)) * t; n.M13 = ((n12 * n23) - (n13 * n22)) * t;
            n.M21 = p21 * t; n.M22 = ((n11 * n33) - (n13 * n31)) * t; n.M23 = ((n13 * n21) - (n11 * n23)) * t;
            n.M31 = p31 * t; n.M32 = ((n12 * n31) - (n11 * n32)) * t; n.M33 = ((n11 * n22) - (n12 * n21)) * t;
        }

        public static Matrix3 Lerp(ref Matrix3 a, ref Matrix3 b, float aWeightOfB)
        {
            Matrix3 ret = new Matrix3(MathHelper.Lerp(a.M11, b.M11, aWeightOfB), MathHelper.Lerp(a.M12, b.M12, aWeightOfB), MathHelper.Lerp(a.M13, b.M13, aWeightOfB),
                                      MathHelper.Lerp(a.M21, b.M21, aWeightOfB), MathHelper.Lerp(a.M22, b.M22, aWeightOfB), MathHelper.Lerp(a.M23, b.M23, aWeightOfB),
                                      MathHelper.Lerp(a.M31, b.M31, aWeightOfB), MathHelper.Lerp(a.M32, b.M32, aWeightOfB), MathHelper.Lerp(a.M33, b.M33, aWeightOfB));

            return ret;
        }

        /// <summary>
        /// Returns an XNA 4x4 Matrix.
        /// </summary>
        /// <returns>An XNA 4x4 Matrix.</returns>
        public Matrix ToMatrix()
        {
            return new Matrix(M11, M12, M13, 0.0f,
                              M21, M22, M23, 0.0f,
                              M31, M32, M33, 0.0f,
                              0.0f, 0.0f, 0.0f, 1.0f);
        }

        public float Trace()
        {
            return (M11 + M22 + M33);
        }

        public override string ToString()
        {
            return "{ {M11:" + M11.ToString() + " M12:" + M12.ToString() + " M13:" + M13.ToString() + "}" +
                     "{M21:" + M21.ToString() + " M22:" + M22.ToString() + " M23:" + M23.ToString() + "}" +
                     "{M31:" + M31.ToString() + " M32:" + M32.ToString() + " M33:" + M33.ToString() + "} }";
        }

        public static void Transform(ref Vector3 u, ref Matrix3 m, out Vector3 v)
        {
            v = new Vector3(u.X * m.M11 + u.Y * m.M21 + u.Z * m.M31,
                            u.X * m.M12 + u.Y * m.M22 + u.Z * m.M32,
                            u.X * m.M13 + u.Y * m.M23 + u.Z * m.M33);
        }   

        /// <summary>
        /// Transforms a vector v by this 3x3 matrix.
        /// </summary>
        /// <param name="v">The vector to transform</param>
        /// <returns>The resulting vector.</returns>
        public Vector3 Transform(Vector3 u)
        {
            return new Vector3(u.X * M11 + u.Y * M21 + u.Z * M31,
                               u.X * M12 + u.Y * M22 + u.Z * M32,
                               u.X * M13 + u.Y * M23 + u.Z * M33);
        }

        public static Matrix3 Transpose(Matrix3 m)
        {
            Matrix3 ret;
            Matrix3.Transpose(ref m, out ret);

            return ret;
        }

        public static void Transpose(ref Matrix3 m, out Matrix3 n)
        {
            float m12 = m.M12;
            float m13 = m.M13;
            float m23 = m.M23;

            n.M11 = m.M11; n.M12 = m.M21; n.M13 = m.M31;
            n.M21 = m12;   n.M22 = m.M22; n.M23 = m.M32;
            n.M31 = m13;   n.M32 = m23;   n.M33 = m.M33;
        }

        public void Transpose()
        {
            Utilities.Swap(ref M21, ref M12);
            Utilities.Swap(ref M31, ref M13);
            Utilities.Swap(ref M32, ref M23);
        }
    }
}
