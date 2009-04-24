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

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace siat
{

    /// <summary>
    /// Used to indicate an axis of a 3d vector.
    /// </summary>
    public enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2,
        W = 3
    }

    /// <summary>
    /// Wrapper to pass/update Matrix structs as objects without incurring boxing/unboxing cost.
    /// </summary>
    public class MatrixWrapper
    {
        public MatrixWrapper() { Matrix = Matrix.Identity; }
        public MatrixWrapper(Matrix m) { Matrix = m; }

        public Matrix Matrix;
    }

    /// <summary>
    /// Wrapper to pass/update Matrix3 structs as objects without incurring boxing/unboxing cost.
    /// </summary>
    public class Matrix3Wrapper
    {
        public Matrix3Wrapper() { Matrix = Matrix3.Identity; }
        public Matrix3Wrapper(Matrix3 m) { Matrix = m; }

        public Matrix3 Matrix;
    }

    /// <summary>
    /// Types of scene nodes explicitly identified in a scene graph.
    /// </summary>
    public enum SceneNodeType
    {
        None,
        AnimatedMeshPart,
        Camera,
        Joint,
        Light,
        Mesh,
        MeshPart,
        Node,
        Portal,
        DirectionalLight,
        Physics,
        PointLight,
        Sky,
        SpotLight
    }

    /// <summary>
    /// Parameters types for effect parameters.
    /// </summary>
    public enum ParameterType
    {
        kSingle,
        kMatrix,
        kVector2,
        kVector3,
        kVector4,
        kTexture
    }

    /// <summary>
    /// Vertex winding.
    /// </summary>
    public enum Winding
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// Miscellaneous utility and math functions.
    /// </summary>
    public static class Utilities
    {
        public static void Assert(bool abAssertion)
        {
#if DEBUG
            if (!abAssertion) throw new Exception("Assertion failed.");
#endif
        }

        public static void AssertEqual<T>(T a, T b)
        {
#if DEBUG
            if (!a.Equals(b)) { throw new Exception(a.ToString() + " is not equal to " + b.ToString()); }
#endif
        }

        #region Constants
#if SIAT_DEFAULT_CLOCKWISE_WINDING
        public const Winding kWinding = Winding.Clockwise;
        public const CullMode kBackFaceCulling = CullMode.CullCounterClockwiseFace; 
        public const CullMode kFrontFaceCulling = CullMode.CullClockwiseFace; 
#elif SIAT_DEFAULT_COUNTER_CLOCKWISE_WINDING
        public const Winding kWinding = Winding.CounterClockwise;
        public const CullMode kBackFaceCulling = CullMode.CullClockwiseFace; 
        public const CullMode kFrontFaceCulling = CullMode.CullCounterClockwiseFace; 
#endif
        public const float kInfiniteFarPlaneEpsilon = 2.4e-7f;
        public const int kFindEigenspaceIterations = 32;
        public const float kJacobiToleranceFloat = 1e-8f;
        public const double kJacobiToleranceDouble = 1e-10;
        public static BoundingBox kZeroBox = default(BoundingBox);
        public static BoundingSphere kZeroSphere = default(BoundingSphere);
        public static Vector3 kZeroVector3 = Vector3.Zero;
        public static Matrix kIdentity = Matrix.Identity;
        public static CoordinateFrame kIdentityFrame = CoordinateFrame.Identity;
        public static MatrixWrapper kIdentityWrapped = new MatrixWrapper();
        public const string kMediaRoot = "media\\";
        public const float kSqrtOneHalf = 0.707106781187f;
        public static Vector3 kUnitX = Vector3.UnitX;
        public static Vector3 kUnitY = Vector3.UnitY;
        public static Vector3 kUnitZ = Vector3.UnitZ;
        public static object kDummy = 0;
        public static readonly Vector3 kMinVector3 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        public static readonly Vector3 kMaxVector3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        public static BoundingBox kMaxBox = new BoundingBox(kMinVector3, kMaxVector3);
        public static BoundingBox kInvertedMaxBox = new BoundingBox(kMaxVector3, kMinVector3);
        public static BoundingSphere kMaxSphere = new BoundingSphere(Vector3.Zero, float.MaxValue);
        public const float kMinLuminance = 0.004f;
        public const string kSingleton = "This class can only have one instance at a time.";
        public const string kShouldNotBeHere = "Execution has reached a code branch that should not be possible. Please send the details of this exception to a developer.";
        public const string kNotImplemented = "Execution has reached code that has not yet been implemented.";
        public const float kLooseToleranceFloat = 1e-03f;
        public const float kNegativeLooseToleranceFloat = -kLooseToleranceFloat;
        public const double kLooseToleranceDouble = 1e-05f;
        public const double kNegativeLooseToleranceDouble = -kLooseToleranceDouble;

        public const float kZeroToleranceFloat = 1e-06f;
        public const double kZeroToleranceDouble = 1e-08;
        #endregion

        #region Comparison
        #region AboutZero
        public static bool AboutZero(float a)
        {
            return (Math.Abs(a) < kZeroToleranceFloat);
        }

        public static bool AboutZero(float a, float aTolerance)
        {
            return (Math.Abs(a) < aTolerance);
        }

        public static bool AboutZero(double a)
        {
            return (Math.Abs(a) < kZeroToleranceDouble);
        }
        
        public static bool AboutZero(double a, double aTolerance)
        {
            return (Math.Abs(a) < aTolerance);
        }

        public static bool AboutZero(Vector3 u)
        {
            return AboutZero(u.X) &&
                   AboutZero(u.Y) &&
                   AboutZero(u.Z);
        }

        public static bool AboutZero(Vector3 u, float aTolerance)
        {
            return AboutZero(u.X, aTolerance) &&
                   AboutZero(u.Y, aTolerance) &&
                   AboutZero(u.Z, aTolerance);
        }

        public static bool AboutZero(ref Vector3 u)
        {
            return AboutZero(u.X) &&
                   AboutZero(u.Y) &&
                   AboutZero(u.Z);
        }

        public static bool AboutZero(ref Vector3 u, float aTolerance)
        {
            return AboutZero(u.X, aTolerance) &&
                   AboutZero(u.Y, aTolerance) &&
                   AboutZero(u.Z, aTolerance);
        }
        #endregion

        #region About Equal
        public static bool AboutEqual(float a, float b)
        {
            return (Math.Abs(a - b) < kZeroToleranceFloat);
        }

        public static bool AboutEqual(float a, float b, float aTolerance)
        {
            return (Math.Abs(a - b) < aTolerance);
        }

        public static bool AboutEqual(double a, double b)
        {
            return (Math.Abs(a - b) < kZeroToleranceDouble);
        }

        public static bool AboutEqual(double a, double b, double aTolerance)
        {
            return (Math.Abs(a - b) < aTolerance);
        }

        public static bool AboutEqual(ref Vector3 u, ref Vector3 v)
        {
            return AboutEqual(u.X, v.X) &&
                   AboutEqual(u.Y, v.Y) &&
                   AboutEqual(u.Z, v.Z);
        }

        public static bool AboutEqual(Vector3 u, Vector3 v)
        {
            return AboutEqual(u.X, v.X) &&
                   AboutEqual(u.Y, v.Y) &&
                   AboutEqual(u.Z, v.Z);
        }

        public static bool AboutEqual(Vector3 u, Vector3 v, float aTolerance)
        {
            return AboutEqual(u.X, v.X, aTolerance) &&
                   AboutEqual(u.Y, v.Y, aTolerance) &&
                   AboutEqual(u.Z, v.Z, aTolerance);
        }

        public static bool AboutEqual(BoundingBox a, BoundingBox b)
        {
            return AboutEqual(ref a.Max, ref b.Max) && AboutEqual(ref a.Min, ref b.Min);
        }

        public static bool AboutEqual(BoundingBox a, BoundingBox b, float aTolerance)
        {
            return AboutEqual(ref a.Max, ref b.Max, aTolerance) && AboutEqual(ref a.Min, ref b.Min, aTolerance);
        }

        public static bool AboutEqual(BoundingSphere a, BoundingSphere b)
        {
            return AboutEqual(ref a.Center, ref b.Center) && AboutEqual(a.Radius, b.Radius);
        }

        public static bool AboutEqual(BoundingSphere a, BoundingSphere b, float aTolerance)
        {
            return AboutEqual(ref a.Center, ref b.Center, aTolerance) && AboutEqual(a.Radius, b.Radius, aTolerance);
        }

        public static bool AboutEqual(ref BoundingSphere a, ref BoundingSphere b)
        {
            return AboutEqual(ref a.Center, ref b.Center) && AboutEqual(a.Radius, b.Radius);
        }

        public static bool AboutEqual(ref BoundingSphere a, ref BoundingSphere b, float aTolerance)
        {
            return AboutEqual(ref a.Center, ref b.Center, aTolerance) && AboutEqual(a.Radius, b.Radius, aTolerance);
        }

        public static bool AboutEqual(ref Quaternion a, ref Quaternion b)
        {
            return AboutEqual(a.X, b.X) && AboutEqual(a.Y, b.Y) && AboutEqual(a.Z, b.Z) && AboutEqual(a.W, b.W);
        }

        public static bool AboutEqual(ref Quaternion a, ref Quaternion b, float aTolerance)
        {
            return AboutEqual(a.X, b.X, aTolerance) && AboutEqual(a.Y, b.Y, aTolerance) && AboutEqual(a.Z, b.Z, aTolerance) && AboutEqual(a.W, b.W, aTolerance);
        }

        public static bool AboutEqual(Quaternion a, Quaternion b)
        {
            return AboutEqual(a.X, b.X) && AboutEqual(a.Y, b.Y) && AboutEqual(a.Z, b.Z) && AboutEqual(a.W, b.W);
        }

        public static bool AboutEqual(Quaternion a, Quaternion b, float aTolerance)
        {
            return AboutEqual(a.X, b.X, aTolerance) && AboutEqual(a.Y, b.Y, aTolerance) && AboutEqual(a.Z, b.Z, aTolerance) && AboutEqual(a.W, b.W, aTolerance);
        }

        public static bool AboutEqual(ref Vector3 u, ref Vector3 v, float aTolerance)
        {
            return AboutEqual(u.X, v.X, aTolerance) &&
                   AboutEqual(u.Y, v.Y, aTolerance) &&
                   AboutEqual(u.Z, v.Z, aTolerance);
        }

        public static bool AboutEqual(ref Matrix3 m, ref Matrix3 n)
        {
            return AboutEqual(m.M11, n.M11) && AboutEqual(m.M12, n.M12) && AboutEqual(m.M13, n.M13) &&
                   AboutEqual(m.M21, n.M21) && AboutEqual(m.M22, n.M22) && AboutEqual(m.M23, n.M23) && 
                   AboutEqual(m.M31, n.M31) && AboutEqual(m.M32, n.M32) && AboutEqual(m.M33, n.M33);
        }

        public static bool AboutEqual(ref Matrix3 m, ref Matrix3 n, float aTolerance)
        {
            return AboutEqual(m.M11, n.M11, aTolerance) && AboutEqual(m.M12, n.M12, aTolerance) && AboutEqual(m.M13, n.M13, aTolerance) &&
                   AboutEqual(m.M21, n.M21, aTolerance) && AboutEqual(m.M22, n.M22, aTolerance) && AboutEqual(m.M23, n.M23, aTolerance) &&
                   AboutEqual(m.M31, n.M31, aTolerance) && AboutEqual(m.M32, n.M32, aTolerance) && AboutEqual(m.M33, n.M33, aTolerance);
        }

        public static bool AboutEqual(ref Matrix m, ref Matrix n)
        {
            return AboutEqual(m.M11, n.M11) && AboutEqual(m.M12, n.M12) && AboutEqual(m.M13, n.M13) && AboutEqual(m.M14, n.M14) &&
                   AboutEqual(m.M21, n.M21) && AboutEqual(m.M22, n.M22) && AboutEqual(m.M23, n.M23) && AboutEqual(m.M24, n.M24) &&
                   AboutEqual(m.M31, n.M31) && AboutEqual(m.M32, n.M32) && AboutEqual(m.M33, n.M33) && AboutEqual(m.M34, n.M34) &&
                   AboutEqual(m.M41, n.M41) && AboutEqual(m.M42, n.M42) && AboutEqual(m.M43, n.M43) && AboutEqual(m.M44, n.M44);
        }

        public static bool AboutEqual(ref Matrix m, ref Matrix n, float aTolerance)
        {
            return AboutEqual(m.M11, n.M11, aTolerance) && AboutEqual(m.M12, n.M12, aTolerance) && AboutEqual(m.M13, n.M13, aTolerance) && AboutEqual(m.M14, n.M14, aTolerance) &&
                   AboutEqual(m.M21, n.M21, aTolerance) && AboutEqual(m.M22, n.M22, aTolerance) && AboutEqual(m.M23, n.M23, aTolerance) && AboutEqual(m.M24, n.M24, aTolerance) &&
                   AboutEqual(m.M31, n.M31, aTolerance) && AboutEqual(m.M32, n.M32, aTolerance) && AboutEqual(m.M33, n.M33, aTolerance) && AboutEqual(m.M34, n.M34, aTolerance) &&
                   AboutEqual(m.M41, n.M41, aTolerance) && AboutEqual(m.M42, n.M42, aTolerance) && AboutEqual(m.M43, n.M43, aTolerance) && AboutEqual(m.M44, n.M44, aTolerance);
        }

        public static bool AboutEqual(Matrix m, Matrix n)
        {
            return AboutEqual(m.M11, n.M11) && AboutEqual(m.M12, n.M12) && AboutEqual(m.M13, n.M13) && AboutEqual(m.M14, n.M14) &&
                   AboutEqual(m.M21, n.M21) && AboutEqual(m.M22, n.M22) && AboutEqual(m.M23, n.M23) && AboutEqual(m.M24, n.M24) &&
                   AboutEqual(m.M31, n.M31) && AboutEqual(m.M32, n.M32) && AboutEqual(m.M33, n.M33) && AboutEqual(m.M34, n.M34) &&
                   AboutEqual(m.M41, n.M41) && AboutEqual(m.M42, n.M42) && AboutEqual(m.M43, n.M43) && AboutEqual(m.M44, n.M44);
        }

        public static bool AboutEqual(Matrix m, Matrix n, float aTolerance)
        {
            return AboutEqual(m.M11, n.M11, aTolerance) && AboutEqual(m.M12, n.M12, aTolerance) && AboutEqual(m.M13, n.M13, aTolerance) && AboutEqual(m.M14, n.M14, aTolerance) &&
                   AboutEqual(m.M21, n.M21, aTolerance) && AboutEqual(m.M22, n.M22, aTolerance) && AboutEqual(m.M23, n.M23, aTolerance) && AboutEqual(m.M24, n.M24, aTolerance) &&
                   AboutEqual(m.M31, n.M31, aTolerance) && AboutEqual(m.M32, n.M32, aTolerance) && AboutEqual(m.M33, n.M33, aTolerance) && AboutEqual(m.M34, n.M34, aTolerance) &&
                   AboutEqual(m.M41, n.M41, aTolerance) && AboutEqual(m.M42, n.M42, aTolerance) && AboutEqual(m.M43, n.M43, aTolerance) && AboutEqual(m.M44, n.M44, aTolerance);
        }
        #endregion

        #region GreaterThan
        public static bool GreaterThan(float a, float b)
        {
            return (a >= (b + kZeroToleranceFloat));
        }

        public static bool GreaterThan(float a, float b, float aTolerance)
        {
            return (a >= (b + aTolerance));
        }

        public static bool GreaterThan(double a, double b)
        {
            return (a >= (b + kZeroToleranceDouble));
        }

        public static bool GreaterThan(double a, double b, double aTolerance)
        {
            return (a >= (b + aTolerance));
        }

        public static bool GreaterThan(Vector3 u, Vector3 v)
        {
            return (GreaterThan(u.X, v.X) && 
                    GreaterThan(u.Y, v.Y) && 
                    GreaterThan(u.Z, v.Z));
        }

        public static bool GreaterThan(Vector3 u, Vector3 v, float aTolerance)
        {
            return (GreaterThan(u.X, v.X, aTolerance) &&
                    GreaterThan(u.Y, v.Y, aTolerance) &&
                    GreaterThan(u.Z, v.Z, aTolerance));
        }
        #endregion

        #region Less Than
        public static bool LessThan(float a, float b)
        {
            return (a <= (b - kZeroToleranceFloat));
        }

        public static bool LessThan(float a, float b, float aTolerance)
        {
            return (a <= (b - aTolerance));
        }

        public static bool LessThan(double a, double b)
        {
            return (a <= (b - kZeroToleranceDouble));
        }

        public static bool LessThan(double a, double b, double aTolerance)
        {
            return (a <= (b - aTolerance));
        }
        
        public static bool LessThan(Vector3 u, Vector3 v)
        {
            return (LessThan(u.X, v.X) && LessThan(u.Y, v.Y) && LessThan(u.Z, v.Z));
        }

        public static bool LessThan(Vector3 u, Vector3 v, float aTolerance)
        {
            return (LessThan(u.X, v.X, aTolerance) &&
                    LessThan(u.Y, v.Y, aTolerance) &&
                    LessThan(u.Z, v.Z, aTolerance));
        }
        #endregion
        #endregion

        #region Min/Max
        public static T Min<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) < 0) { return a; }
            else { return b; }
        }

        public static T Min<T>(T a, T b, T c) where T : IComparable<T>
        {
            return Min(a, Min(b, c));
        }

        public static T Min<T>(T a, T b, T c, T d) where T : IComparable<T>
        {
            return Min(a, b, Min(c, d));
        }

        public static float Min(ref Vector3 v)
        {
            return Min(v.X, v.Y, v.Z);
        }

        public static float Min(ref Vector4 v)
        {
            return Min(v.X, v.Y, v.Z, v.W);
        }

        public static T Max<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) > 0) { return a; }
            else { return b; }
        }

        public static T Max<T>(T a, T b, T c) where T : IComparable<T>
        {
            return Max(a, Max(b, c));
        }

        public static T Max<T>(T a, T b, T c, T d) where T : IComparable<T>
        {
            return Max(a, b, Max(c, d));
        }

        public static float Max(ref Vector3 v)
        {
            return Max(v.X, v.Y, v.Z);
        }

        public static float Max(Vector3 v)
        {
            return Max(v.X, v.Y, v.Z);
        }

        public static float Max(ref Vector4 v)
        {
            return Max(v.X, v.Y, v.Z, v.W);
        }

        public static float Max(Vector4 v)
        {
            return Max(v.X, v.Y, v.Z, v.W);
        }
        #endregion

        #region Miscellaneous
        public static T[] Combine<T>(T[] a, T[] b)
        {
            T[] ret = new T[a.Length + b.Length];
            a.CopyTo(ret, 0);
            b.CopyTo(ret, a.Length);

            return ret;
        }

        public static void Remove<T>(ref T[] a, int aIndex)
        {
            T[] ret = new T[a.Length - 1];
            Array.Copy(a, 0, ret, 0, aIndex);
            Array.Copy(a, aIndex + 1, ret, aIndex, (a.Length - (aIndex + 1)));

            a = ret;
        }

        /// <summary>
        /// Replaces any '\\' characters with '\\\\'.
        /// </summary>
        /// <param name="aFilePath"></param>
        /// <returns></returns>
        public static string ParseFilePathForFileWrite(string aFilePath)
        {
            string ret = string.Empty;
            if (System.IO.Path.DirectorySeparatorChar == '\\')
            {
                string[] split = aFilePath.Split(System.IO.Path.DirectorySeparatorChar);
                int length = split.Length;

                if (length > 0)
                {
                    ret = split[0];

                    for (int i = 1; i < length; i++)
                    {
                        ret += "\\\\" + split[i];
                    }
                }
            }

            return ret;
        }

        public static T Clamp<T>(T a, T aMin, T aMax) where T : IComparable<T>
        {
            if (a.CompareTo(aMax) > 0) { return aMax; }
            else if (a.CompareTo(aMin) < 0) { return aMin; }
            else { return a; }
        }

        public static void Clamp(ref BoundingBox aAABB, ref BoundingBox aMinMax, out BoundingBox arAABB)
        {
            Vector3.Min(ref aAABB.Max, ref aMinMax.Max, out arAABB.Max);
            Vector3.Max(ref aAABB.Min, ref aMinMax.Min, out arAABB.Min);
        }

        public static ContainmentType Contains(SiatPlane[] aPlanes, ref BoundingBox aBox)
        {
            ContainmentType ret = ContainmentType.Contains;
            Vector3 rst = (aBox.Max - aBox.Min);
            Vector3 center = Utilities.GetCenter(ref aBox);

            int count = aPlanes.Length;
            for (int i = 0; i < count; i++)
            {
                float dot;
                Vector3.Dot(ref rst, ref aPlanes[i].AbsNormal, out dot);

                float radius = 0.5f * dot;
                float negRadius = -radius;

                float d;
                aPlanes[i].Plane.DotCoordinate(ref center, out d);

                if (d < negRadius) { return ContainmentType.Disjoint; }
                else if (d <= radius) { ret = ContainmentType.Intersects; }
            }

            return ret;
        }

        /// <summary>
        /// Calculates the near and far distances from an XNA standard projection transform.
        /// </summary>
        /// <param name="aProjectionTransform">The projection transform.</param>
        /// <param name="aNear">Distance to near plane.</param>
        /// <param name="aFar">Distance to far plane.</param>
        public static void ExtractNearFar(ref Matrix aProjectionTransform, out float aNear, out float aFar)
        {
            aNear = aProjectionTransform.M43 / aProjectionTransform.M33;
            aFar = aProjectionTransform.M43 / (1.0f + aProjectionTransform.M33);
        }

        public static void ExtractNearFar(Matrix aProjectionTransform, out float aNear, out float aFar)
        {
            aNear = aProjectionTransform.M43 / aProjectionTransform.M33;
            aFar = aProjectionTransform.M43 / (1.0f + aProjectionTransform.M33);
        }

        public static float ExtractFov(ref Matrix aProjectionTransform)
        {
            float ret = 2.0f * ((float)Math.Atan(1.0f / aProjectionTransform.M22));

            return ret;
        }

        public static float ExtractFov(Matrix aProjectionTransform)
        {
            float ret = 2.0f * ((float)Math.Atan(1.0f / aProjectionTransform.M22));

            return ret;
        }

        /// <summary>
        /// Returns a uint representation of a float that has the same sort order as the float.
        /// </summary>
        /// <param name="s">The float.</param>
        /// <returns>The encoded float.</returns>
        /// <remarks>
        /// Reinterpreting the bits of a float as an int results in a number with a different
        /// sort order than the float. This function manipulates the float such that the resulting
        /// uint will sort in the same order as the original float values. The uint is effectively
        /// garbage until deencoded, it is only useful for sorting.
        /// 
        /// From: http://www.codercorner.com/SAP.pdf
        /// </remarks>
        /// 
        /// \todo Check.
        public static uint GetSortableUintFromFloat(float s)
        {
            unsafe
            {
                uint i = *((uint*)&s);

                // 0x80000000 is hex for the sign bit of an IEEE floating point number.
                // if negative, reverse sequence
                if ((i & 0x80000000) != 0) { i = ~i; }
                // otherwise, flip sign.
                else { i |= 0x80000000; }

                return i;
            }
        }

        /// <summary>
        /// Returns a float from a sortable uint representation. See GetSortableUintFromFloat().
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static float GetFloatFromSortableUint(uint i)
        {
            unsafe
            {
                // 0x80000000 is hex for the sign bit of an IEEE floating point number.
                // if positive, flip sign
                if ((i & 0x80000000) != 0) { i &= ~0x80000000; }
                // otherwise, reverse sequence.
                else { i = ~i; }

                float ret = *((float*)i);

                return ret;
            }
        }

        public static readonly float kMaxLightRange = (float)(Math.Sqrt(float.MaxValue * 0.5f));

        public static float GetLightRange(ref Vector3 aLightAttenuation, ref Vector3 aLightDiffuse, ref Vector3 aLightSpecular)
        {
            float max = Utilities.Max(Utilities.GetLuminance(aLightDiffuse), Utilities.GetLuminance(aLightDiffuse));

            float ret = 0.0f;
            if (!Utilities.AboutZero(aLightAttenuation.Z))
            {
                // max / (x + (y * d) + (z * d * d)) = kMinLuminance
                // (max / kMinLuminance) = (x + (y * d) + (z * d * d))
                // 0 = ((x - (max / kMinLuminance)) + (y * d) + (z * d * d))
                float a = aLightAttenuation.Z;
                float b = aLightAttenuation.Y;
                float c = (aLightAttenuation.X - (max / kMinLuminance));

                ret = (-b + (float)Math.Sqrt((b * b) - (4 * a * c))) / (2.0f * a);
            }
            else if (!Utilities.AboutZero(aLightAttenuation.Y))
            {
                ret = ((max / kMinLuminance) - aLightAttenuation.X) / aLightAttenuation.Y;
            }
            else if (!Utilities.AboutZero(aLightAttenuation.X))
            {
                ret = kMaxLightRange;
            }

            return ret;
        }

        public static void ConvertToGrayscale(int aWidth, int aHeight, SurfaceFormat aFormat, byte[] aIn, out byte[] arOut)
        {
            arOut = new byte[aWidth * aHeight];

            int stride = Utilities.GetStride(aFormat);
            int size = aWidth * aHeight * stride;

            for (int i = 0, index = 0; i < size; i += stride, index++)
            {
                Color color = Utilities.GetPixel(aFormat, aIn, i);
                arOut[index] = GetYofCIE1931sRGB(ref color);
            }
        }

        public static void FlipVertical(int aWidth, int aHeight, SurfaceFormat aFormat, byte[] aIn, out byte[] arOut)
        {
            int mid = (aHeight / 2);
            int stride = Utilities.GetStride(aFormat);
            int size = aWidth * aHeight * stride;
            int pitch = aWidth * stride;

            arOut = new byte[size];

            for (int y = 0; y < mid; y++)
            {
                for (int x = 0; x < pitch; x++)
                {
                    int kFromIndex = (y * pitch) + x;
                    int kToIndex = (size - pitch) - (y * pitch) + x;

                    arOut[kToIndex] = aIn[kFromIndex];
                    arOut[kFromIndex] = aIn[kToIndex];
                }
            }
        }

        public static Plane CreateFromNormalAndPosition(ref Vector3 aNormal, ref Vector3 aPosition)
        {
            return new Plane(aNormal, -Vector3.Dot(aNormal, aPosition));
        }

        public static Color GetPixel(SurfaceFormat aFormat, byte[] p, int index)
        {
            switch (aFormat)
            {
                case SurfaceFormat.Color: return new Color(p[index + 2], p[index + 1], p[index + 0]);
                case SurfaceFormat.Bgr24: goto case SurfaceFormat.Bgr32;
                case SurfaceFormat.Bgr32: return new Color(p[index + 0], p[index + 1], p[index + 2]);
                case SurfaceFormat.Rgb32: goto case SurfaceFormat.Rgba32;
                case SurfaceFormat.Rgba32: return new Color(p[index + 2], p[index + 1], p[index + 0]);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void SetPixel(SurfaceFormat aFormat, byte[] p, int index, Color aColor)
        {
            switch (aFormat)
            {
                case SurfaceFormat.Color: p[index + 3] = aColor.A; p[index + 2] = aColor.R; p[index + 1] = aColor.G; p[index + 0] = aColor.B; break;
                case SurfaceFormat.Bgr24: p[index + 0] = aColor.R; p[index + 1] = aColor.G; p[index + 2] = aColor.B; break;
                case SurfaceFormat.Bgr32: p[index + 0] = aColor.R; p[index + 1] = aColor.G; p[index + 2] = aColor.B; p[index + 3] = aColor.A; break;
                case SurfaceFormat.Rgb32: goto case SurfaceFormat.Rgba32;
                case SurfaceFormat.Rgba32: p[index + 3] = aColor.A; p[index + 2] = aColor.R; p[index + 1] = aColor.G; p[index + 0] = aColor.B; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int GetStride(SurfaceFormat aFormat)
        {
            switch (aFormat)
            {
                case SurfaceFormat.Color: return 4;
                case SurfaceFormat.Bgr24: return 3;
                case SurfaceFormat.Bgr32: return 4;
                case SurfaceFormat.Luminance8: return 1;
                case SurfaceFormat.Rgb32: return 4; // verify that this is really 32-bit.
                case SurfaceFormat.Rgba32: return 4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public const double kYfromRgbAlpha = 0.055;
        public const double kYfromRgbPhi= 12.92;
        public const double kYfromRgbTau = 2.4;
        
        public static double GetYfromRgbHelper(double v)
        {
            const double k = 1.0 + kYfromRgbAlpha;
            
            if (v > 0.04045)
            {
                return Math.Pow((v + kYfromRgbAlpha) / k, kYfromRgbTau);    
            }
            else
            {
                return (v  / kYfromRgbPhi);
            }
        }
        
        public static float GetYfromRgbHelper(float v)
        {
            return (float)GetYfromRgbHelper((double)v);
        }

        public static double GetYofCIE1931sRGB(double r, double g, double b)
        {
            return ((0.2126 * GetYfromRgbHelper(r)) + (0.7152 * GetYfromRgbHelper(g)) + (0.0722 * GetYfromRgbHelper(b)));
        }

        public static float GetYofCIE1931sRGB(float r, float g, float b)
        {
            return ((0.2126f * GetYfromRgbHelper(r)) + (0.7152f * GetYfromRgbHelper(g)) + (0.0722f * GetYfromRgbHelper(b)));
        }

        public static byte GetYofCIE1931sRGB(byte r, byte g, byte b)
        {
            const float kToFactor = (float)(1.0 / 255.0);
            const float kFromFactor = (float)(255.0);
            
            float fR = ((float)r) * kToFactor;
            float fG = ((float)g) * kToFactor;
            float fB = ((float)b) * kToFactor;

            return (byte)(GetYofCIE1931sRGB(fR, fG, fB) * kFromFactor);
        }

        public static byte GetYofCIE1931sRGB(ref Color c)
        {
            return GetYofCIE1931sRGB(c.R, c.G, c.B);
        }
        
        public static bool Intersect(ref Triangle aTriangle, ref Vector3 aPoint)
        {
            Vector3 v0 = (aTriangle.P1 - aTriangle.P0);
            Vector3 v1 = (aTriangle.P2 - aTriangle.P0);
            Vector3 v2 = (aPoint - aTriangle.P1);

            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            // Barycentric coordinates: http://en.wikipedia.org/wiki/Barycentric_coordinates_(mathematics)
            float invD = 1.0f / ((dot00 * dot11) - (dot01 * dot01));
            float u = ((dot11 * dot02) - (dot01 * dot12)) * invD;
            float v = ((dot00 * dot12) - (dot01 * dot02)) * invD;

            float uv = (u + v);

            bool bReturn = ((u > 0.0f) || AboutZero(u));
            bReturn = bReturn && ((v > 0.0f) || AboutZero(v));
            bReturn = bReturn && ((uv < 1.0f) || AboutEqual(uv, 1.0f));

            return bReturn;
        }

        /// <summary>
        /// Intersects a ray with a triangle, taking into account the front face of the triangle.
        /// </summary>
        /// <param name="aRay">The ray to intersect.</param>
        /// <param name="p1">Vertex 1 of the triangle.</param>
        /// <param name="p2">Vertex 2 of the triangle.</param>
        /// <param name="p3">Vertex 3 of the triangle.</param>
        /// <returns>Depth of intersection or null if no intersection.</returns>
        public static float? Intersect(ref Triangle aTriangle, ref Ray aRay)
        {
            Plane plane = aTriangle.Plane;
            float? f = aRay.Intersects(plane);

            if (f != null)
            {
                Vector3 p = aRay.Position + (((float)f) * aRay.Direction);

                if (Intersect(ref aTriangle, ref p)) { return f; }
            }

            return null;
        }

        /// <summary>
        /// ax2 + bx + c = 0
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="arMinusX"></param>
        /// <param name="arPlusX"></param>
        public static void QuadraticFormula(float a, float b, float c, out float arMinusX, out float arPlusX)
        {
            float bSquare = (b * b);
            float ac = (a * c);
            float inv2a = 1.0f / (2.0f * a);

            float t = (float)Math.Sqrt(bSquare - (4.0f * ac));

            arMinusX = (-b - t) * inv2a;
            arPlusX = (-b + t) * inv2a;
        }

        public static PlaneIntersectionType Intersect(ref Plane aPlane, ref Vector3 aPoint, float aTolerance)
        {
            float d = aPlane.DotCoordinate(aPoint);
            if (GreaterThan(d, aTolerance)) return PlaneIntersectionType.Front;
            else if (LessThan(d, aTolerance)) return PlaneIntersectionType.Back;
            else return PlaneIntersectionType.Intersecting;
        }

        public static PlaneIntersectionType Intersect(ref Plane aPlane, ref Vector3 aPoint)
        {
            return Intersect(ref aPlane, ref aPoint, kLooseToleranceFloat);
        }

        public static string RemoveExtension(string aPath)
        {
            string directory = System.IO.Path.GetDirectoryName(aPath);
            string ret = string.Empty;

            if (directory != string.Empty)
            {
                ret = directory + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileNameWithoutExtension(aPath);
            }
            else
            {
                ret = System.IO.Path.GetFileNameWithoutExtension(aPath);
            }

            return ret;
        }

        public static Vector3 SafeNormalize(Vector3 v)
        {
            float len = v.Length();

            if (!Utilities.AboutZero(len)) { return (v / len); }
            else { return v; }
        }

        public static Vector2 SafeNormalize(Vector2 v)
        {
            float len = v.Length();

            if (!Utilities.AboutZero(len)) { return (v / len); }
            else { return v; }
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp;
            tmp = a;
            a = b;
            b = tmp;
        }

        public delegate T TokenizeConvertFunction<T>(string aToken);
        public static void Tokenize<T>(string aInput, T[] aArray, TokenizeConvertFunction<T> aFunc)
        {
            string input = aInput.Trim();
            int numFound = 0;
            int i = 0;
            int length = input.Length;
            int expectedCount = aArray.Length;

            while (i < length && numFound < expectedCount)
            {
                string token = "";

                while (i < length && !char.IsWhiteSpace(input[i]))
                {
                    token += input[i];
                    i++;
                }

                aArray[numFound] = aFunc(token);
                numFound++;

                while (i < length && char.IsWhiteSpace(input[i]))
                {
                    i++;
                }
            }

            if (i != length || numFound != expectedCount)
            {
                throw new Exception("Token count was not expected number.");
            }
        }

        public static void Tokenize(string aInput, string[] aArray)
        {
            Tokenize<string>(aInput, aArray, delegate(String aIn) { return aIn; });
        }

        public static T[] Tokenize<T>(string aInput, TokenizeConvertFunction<T> aFunc)
        {
            List<T> buf = new List<T>();

            string input = aInput.Trim();
            int i = 0;
            int length = input.Length;

            while (i < length)
            {
                string token = "";

                while (i < length && !char.IsWhiteSpace(input[i]))
                {
                    token += input[i];
                    i++;
                }

                buf.Add(aFunc(token));

                while (i < length && char.IsWhiteSpace(input[i]))
                {
                    i++;
                }
            }

            if (i != length)
            {
                throw new Exception("Tokenizing ended before string end.");
            }

            return buf.ToArray();
        }

        public static string[] Tokenize(string aInput)
        {
            return Tokenize<string>(aInput, delegate(String aIn) { return aIn; });
        }

        public static Vector3 Project(Vector3 aPoint, Viewport aViewport, Matrix aViewProjection)
        {
            Vector4 u = new Vector4(aPoint, 1.0f);
            Vector4.Transform(ref u, ref aViewProjection, out u);
            u /= u.W;

            float width = (float)aViewport.Width;
            float height = (float)aViewport.Height;

            float mouseX = ((u.X + 1.0f) * 0.5f) * (width - 1.0f);
            float mouseY = ((u.Y + 1.0f) * 0.5f) * (height - 1.0f);

            return new Vector3(mouseX, mouseY, u.Z);
        }

        public static Vector3 UnProject(Vector3 aPoint, Viewport aViewport, Matrix aInverseViewProjection)
        {
            int width = aViewport.Width;
            int height = aViewport.Height;

            float x = (float)(2.0 * (((double)aPoint.X) / ((double)(width - 1))) - 1.0);
            float y = (float)(2.0 * (1.0 - (((double)aPoint.Y) / ((double)(height - 1)))) - 1.0);

            Vector4 u = new Vector4(x, y, aPoint.Z, 1.0f);
            Vector4.Transform(ref u, ref aInverseViewProjection, out u);
            u /= u.W;

            return ToVector3(u);
        }
        #endregion

        #region Math and Linear Algebra
        public static float Gaussian1D(float dx, float aStdDev)
        {
            double dx2 = (double)(dx * dx);
            double stdDev2 = (double)(aStdDev * aStdDev);

            double kFactor = (1.0 / Math.Sqrt(2.0 * Math.PI * stdDev2));
            double ret = kFactor * Math.Exp(-dx2 / (2.0 * stdDev2));

            return (float)ret;
        }

        public static void Transform(ref Vector3 u, ref Quaternion q, out Vector3 arOut)
        {
            Vector3 uv, uuv;
            Vector3 qvec = new Vector3(q.X, q.Y, q.Z);

            uv = Vector3.Cross(qvec, u);
            uuv = Vector3.Cross(qvec, uv);

            uv *= (2.0f * q.W);
            uuv *= 2.0f;

            arOut = (u + uv + uuv);
        }

        public static Matrix3 Abs(ref Matrix3 m)
        {
            return new Matrix3(Math.Abs(m.M11), Math.Abs(m.M12), Math.Abs(m.M13),
                               Math.Abs(m.M21), Math.Abs(m.M22), Math.Abs(m.M23),
                               Math.Abs(m.M31), Math.Abs(m.M32), Math.Abs(m.M33));
        }

        public static Matrix Abs(ref Matrix m)
        {
            return new Matrix(Math.Abs(m.M11), Math.Abs(m.M12), Math.Abs(m.M13), Math.Abs(m.M14),
                              Math.Abs(m.M21), Math.Abs(m.M22), Math.Abs(m.M23), Math.Abs(m.M24),
                              Math.Abs(m.M31), Math.Abs(m.M32), Math.Abs(m.M33), Math.Abs(m.M34),
                              Math.Abs(m.M41), Math.Abs(m.M42), Math.Abs(m.M43), Math.Abs(m.M44));
        }

        public static Vector3 Abs(ref Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static Vector4 Abs(ref Vector4 v)
        {
            return new Vector4(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z), Math.Abs(v.W));
        }

        public static Axis GetClosestAxis(ref Vector3 v)
        {
            return GetMaxAxis(Abs(ref v));
        }

        public static Axis GetClosestAxis(ref Vector4 v)
        {
            return GetMaxAxis(Abs(ref v));
        }

        public static Vector3 ComputeIntersection(ref Vector3 v1, ref Vector3 v2, ref Plane aPlane)
        {
            Vector3 diff = v2 - v1;
            float a = aPlane.DotNormal(diff);
            float t = -(aPlane.DotCoordinate(v1) / a);

            Vector3 ret = v1 + (diff * t);
            return ret;
        }

        // Effective radius from "Mathematics for 3D Game Programming and Computer Graphics",
        // Eric Lengyel, page 235.
        public static float EffectiveRadius(ref OrientedBoundingBox aOBB, ref Vector3 aDirection)
        {
            float ret = Math.Abs(Vector3.Dot(aOBB.Rmag, aDirection)) + Math.Abs(Vector3.Dot(aOBB.Smag, aDirection)) + Math.Abs(Vector3.Dot(aOBB.Tmag, aDirection));

            return ret;
        }

        public static float EffectiveRadius(ref OrientedBoundingBox aOBB, ref Plane aPlane)
        {
            return EffectiveRadius(ref aOBB, ref aPlane.Normal);
        }

        public static float EffectiveRadius(ref BoundingBox aAABB, ref Vector3 aDirection)
        {
            Vector3 rst = aAABB.Max - aAABB.Min;
            Vector3 absNormal = Abs(ref aDirection);

            float dot;
            Vector3.Dot(ref rst, ref absNormal, out dot);

            float effectiveRadius = 0.5f * dot;
            return effectiveRadius;
        }

        public static float EffectiveRadius(ref BoundingBox aAABB, ref Plane aPlane)
        {
            return EffectiveRadius(ref aAABB, ref aPlane.Normal);
        }

        public static Axis GetFurthestAxis(ref Vector3 v)
        {
            return GetMinAxis(Abs(ref v));
        }

        public static Axis GetFurthestAxis(ref Vector4 v)
        {
            return GetMinAxis(Abs(ref v));
        }

        public static Radian GetAngle(ref Quaternion q)
        {
            Radian ret = new Radian(2.0f * (float)Math.Acos(q.W));

            return ret;
        }

        public static Vector3 GetAxis(ref Quaternion q)
        {
            Vector3 ret = new Vector3(q.X, q.Y, q.Z);
            float length = ret.Length();

            if (AboutZero(length))
            {
                ret = Vector3.UnitX;
            }
            else
            {
                ret /= length;
            }

            return ret;
        }

        public static Vector3 GetVectorFromAxis(Axis aAxis)
        {
            switch (aAxis)
            {
                case Axis.X: return Vector3.UnitX;
                case Axis.Y: return Vector3.UnitY;
                case Axis.Z: return Vector3.UnitZ;
                default:
                    throw new Exception(Utilities.kShouldNotBeHere);
            }
        }

        public static Vector3 GetCenter(BoundingBox aAABB)
        {
            return 0.5f * (aAABB.Max + aAABB.Min);
        }

        public static Vector3 GetCenter(ref BoundingBox aAABB)
        {
            return 0.5f * (aAABB.Max + aAABB.Min);
        }

        public static Vector3 GetExtents(BoundingBox aAABB)
        {
            return (aAABB.Max - aAABB.Min);
        }

        public static Vector3 GetExtents(ref BoundingBox aAABB)
        {
            return (aAABB.Max - aAABB.Min);
        }

        public static Vector3 GetHalfExtents(BoundingBox aAABB)
        {
            return 0.5f * GetExtents(aAABB);
        }

        public static Vector3 GetHalfExtents(ref BoundingBox aAABB)
        {
            return 0.5f * GetExtents(ref aAABB);
        }

        public static float GetDiagonal(BoundingBox aAABB)
        {
            float ret;
            Vector3.Distance(ref aAABB.Max, ref aAABB.Min, out ret);

            return ret;
        }

        public static float GetDiagonal(ref BoundingBox aAABB)
        {
            float ret;
            Vector3.Distance(ref aAABB.Max, ref aAABB.Min, out ret);

            return ret;
        }

        public static float GetLuminance(Vector3 aColor)
        {
            float max = Max(aColor.X, aColor.Y, aColor.Z);
            float min = Min(aColor.X, aColor.Y, aColor.Z);

            return (max + min) * 0.5f;
        }

        public static float GetLuminance(ref Vector3 aColor)
        {
            float max = Max(aColor.X, aColor.Y, aColor.Z);
            float min = Min(aColor.X, aColor.Y, aColor.Z);

            return (max + min) * 0.5f;
        }

        public static float GetLuminance(ref Vector4 aColor)
        {
            float max = Max(aColor.X, aColor.Y, aColor.Z);
            float min = Min(aColor.X, aColor.Y, aColor.Z);

            return (max + min) * 0.5f;
        }

        public static float GetSurfaceArea(BoundingBox aBox)
        {
            Vector3 whd = aBox.Max - aBox.Min;
            float sa = 2.0f * ((whd.Z * whd.X) + (whd.Z * whd.Y) + (whd.X * whd.Y));

            return sa;
        }

        public static float GetSurfaceArea(ref BoundingBox aBox)
        {
            Vector3 whd = aBox.Max - aBox.Min;
            float sa = 2.0f * ((whd.Z * whd.X) + (whd.Z * whd.Y) + (whd.X * whd.Y));

            return sa;
        }

        public static float GetInverseSurfaceArea(ref BoundingBox aBox)
        {
            Vector3 whd = aBox.Max - aBox.Min;
            float invSa = 0.5f / ((whd.Z * whd.X) + (whd.Z * whd.Y) + (whd.X * whd.Y));

            return invSa;
        }

        public static float GetElement(Vector3 v, int i)
        {
            switch (i)
            {
                case 0: return v.X;
                case 1: return v.Y;
                case 2: return v.Z;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void SetElement(ref Vector3 v, int i, float s)
        {
            switch (i)
            {
                case 0: v.X = s; break;
                case 1: v.Y = s; break;
                case 2: v.Z = s; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static float GetVolume(ref BoundingBox aBox)
        {
            Vector3 v = (aBox.Max - aBox.Min);

            return (v.X * v.Y * v.Z);
        }

        public static bool IsOrthogonal(ref Matrix m)
        {
            Matrix3 transpose = Matrix3.CreateFromUpperLeft(ref m);
            Matrix3 inverse;
            Matrix3.Invert(ref transpose, out inverse);
            transpose.Transpose();

            return AboutEqual(ref transpose, ref inverse);
        }

        /// <summary>
        /// Calculates a normal matrix from a transform matrix. Assumes the
        /// transform matrix is a general SRT transform.
        /// </summary>
        /// <param name="m">Input matrix.</param>
        /// <param name="arOut">The output normal matrix.</param>
        public static void GetNormalMatrix(ref Matrix m, out Matrix arOut)
        {
            Matrix3 transpose = Matrix3.CreateFromUpperLeft(ref m);
            Matrix3 inverse;
            Matrix3.Invert(ref transpose, out inverse);
            transpose.Transpose();

            if (AboutEqual(ref transpose, ref inverse))
            {
                arOut = m;
            }
            else
            {
                // Note: transposing the inverse in-place.
                arOut.M11 = inverse.M11; arOut.M12 = inverse.M21; arOut.M13 = inverse.M31; arOut.M14 = 0;
                arOut.M21 = inverse.M12; arOut.M22 = inverse.M22; arOut.M23 = inverse.M32; arOut.M24 = 0;
                arOut.M31 = inverse.M13; arOut.M32 = inverse.M23; arOut.M33 = inverse.M33; arOut.M34 = 0;
                arOut.M41 = m.M41; arOut.M42 = m.M42; arOut.M43 = m.M43; arOut.M44 = 1;
            }
        }

        public static bool IsReflection(ref Matrix m)
        {
            bool bReturn = LessThan(UpperLeft3x3Determinant(ref m), 0.0f);
            return bReturn;
        }

        public static Axis GetMaxAxis(Vector3 v)
        {
            Axis ret = Axis.X;
            float maxF = float.MinValue;

            if (v.X > maxF) { ret = Axis.X; maxF = v.X; }
            if (v.Y > maxF) { ret = Axis.Y; maxF = v.Y; }
            if (v.Z > maxF) { ret = Axis.Z; maxF = v.Z; }

            return ret;
        }

        public static Axis GetMinAxis(Vector3 v)
        {
            Axis ret = Axis.X;
            float minF = float.MaxValue;

            if (v.X < minF) { ret = Axis.X; minF = v.X; }
            if (v.Y < minF) { ret = Axis.Y; minF = v.Y; }
            if (v.Z < minF) { ret = Axis.Z; minF = v.Z; }

            return ret;
        }

        public static Axis GetMaxAxis(Vector4 v)
        {
            Axis ret = Axis.X;
            float maxF = float.MinValue;

            if (v.X > maxF) { ret = Axis.X; maxF = v.X; }
            if (v.Y > maxF) { ret = Axis.Y; maxF = v.Y; }
            if (v.Z > maxF) { ret = Axis.Z; maxF = v.Z; }
            if (v.W > maxF) { ret = Axis.W; maxF = v.W; }

            return ret;
        }

        public static Axis GetMinAxis(Vector4 v)
        {
            Axis ret = Axis.X;
            float minF = float.MaxValue;

            if (v.X < minF) { ret = Axis.X; minF = v.X; }
            if (v.Y < minF) { ret = Axis.Y; minF = v.Y; }
            if (v.Z < minF) { ret = Axis.Z; minF = v.Z; }
            if (v.W < minF) { ret = Axis.W; minF = v.W; }

            return ret;
        }

        public static float Mean(float a, float b)
        {
            return 0.5f * (a + b);
        }

        public static float Mean(float a, float b, float c)
        {
            return 0.5f * (Mean(a, b) + c);
        }

        public static Vector3 Mean(Vector3[] a)
        {
            int count = a.Length;
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;

            if (count > 0)
            {
                x = a[0].X;
                y = a[0].Y;
                z = a[0].Z;
            }

            for (int i = 1; i < count; i++)
            {
                x = (x + a[i].X) * 0.5f;
                y = (y + a[i].Y) * 0.5f;
                z = (z + a[i].Z) * 0.5f;
            }

            Vector3 ret = new Vector3(x, y, z);

            return ret;
        }

        public static Vector3 ProjectOnto(ref Vector3 u, ref Plane aPlane)
        {
            float d;
            aPlane.DotCoordinate(ref u, out d);

            Vector3 ret = (u - (d * aPlane.Normal));

            return ret;
        }

        public static Vector3 ProjectOnto(Vector3 u, Plane aPlane)
        {
            float d;
            aPlane.DotCoordinate(ref u, out d);

            Vector3 ret = (u - (d * aPlane.Normal));

            return ret;
        }

        public static float SmallestAngle(Vector3 u, Vector3 v)
        {
            if (AboutZero(ref u) || AboutZero(ref v))
            {
                throw new DivideByZeroException();
            }

            Vector3 normU = Vector3.Normalize(u);
            Vector3 normV = Vector3.Normalize(v);

            return (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(normU, normV), -1.0f, 1.0f));
        }

        private static readonly int[] kNext = new int[] { 1, 2, 0 };

        public static void FromMatrix(ref Matrix3 m, out Quaternion q)
        {
            float d = m.M11 + m.M22 + m.M33;
            float r;
            
            if (GreaterThan(d, 0.0f))
            {
                r = (float)Math.Sqrt(d + 1.0f);
                q.W = 0.5f * r;
                
                r = 0.5f / r;
                q.X = (m.M23 - m.M32) * r;
                q.Y = (m.M31 - m.M13) * r;
                q.Z = (m.M12 - m.M21) * r;
            }
            else
            {
                int i = 0;
                
                if (GreaterThan(m.M22, m.M11)) { i = 1; }
                if (GreaterThan(m.M33, m[i, i])) { i = 2; }

                int j = kNext[i];
                int k = kNext[j];

                r = (float)Math.Sqrt(m[i, i] - m[j, j] - m[k, k] + 1.0f);

                unsafe
                {
                    fixed (float* pX = &(q.X), pY = &(q.Y), pZ = &(q.Z))
                    {
                        float*[] pa = new float*[] { pX, pY, pZ };

                        *pa[i] = 0.5f * r;

                        r = 0.5f / r;
                        q.W = (m[j, k] - m[k, j]) * r;

                        *pa[j] = (m[i, j] + m[j, i]) * r;
                        *pa[k] = (m[i, k] + m[k, i]) * r;
                    }
                }
            }
        }

        /// <summary>
        /// Quaternion to Matrix3 conversion.
        /// </summary>
        /// <param name="q">Quatenrion.</param>
        /// <param name="m">Resulting matrix m.</param>
        /// <remarks>
        /// From Shoemake, 1985.
        /// </remarks>
        public static void ToMatrix(ref Quaternion q, out Matrix3 m)
        {
            float x = 2.0f * q.X;
            float y = 2.0f * q.Y;
            float z = 2.0f * q.Z;

            float wx = q.W * x;
            float wy = q.W * y;
            float wz = q.W * z;
            float xx = q.X * x;
            float xy = q.X * y;
            float xz = q.X * z;
            float yy = q.Y * y;
            float yz = q.Y * z;
            float zz = q.Z * z;

            m.M11 = 1.0f - (yy + zz);
            m.M21 = (xy - wz);
            m.M31 = (xz + wy);

            m.M12 = (xy + wz);
            m.M22 = 1.0f - (xx + zz);
            m.M32 = (yz - wx);

            m.M13 = (xz - wy);
            m.M23 = (yz + wx);
            m.M33 = 1.0f - (xx + yy);
        }

        /// <summary>
        /// Quaternion to Matrix conversion.
        /// </summary>
        /// <param name="q">Quatenrion.</param>
        /// <param name="m">Resulting matrix m.</param>
        /// <remarks>
        /// From Shoemake, 1985.
        /// </remarks>
        public static void ToMatrix(ref Quaternion q, ref Matrix m)
        {
            float x = 2.0f * q.X;
            float y = 2.0f * q.Y;
            float z = 2.0f * q.Z;

            float wx = q.W * x;
            float wy = q.W * y;
            float wz = q.W * z;
            float xx = q.X * x;
            float xy = q.X * y;
            float xz = q.X * z;
            float yy = q.Y * y;
            float yz = q.Y * z;
            float zz = q.Z * z;

            m.M11 = 1.0f - (yy + zz);
            m.M21 = (xy - wz);
            m.M31 = (xz + wy);

            m.M12 = (xy + wz);
            m.M22 = 1.0f - (xx + zz);
            m.M32 = (yz - wx);

            m.M13 = (xz - wy);
            m.M23 = (yz + wx);
            m.M33 = 1.0f - (xx + yy);
        }

        public static void ToMatrix(ref Vector3 v, ref Matrix m)
        {
            m.M41 = v.X;
            m.M42 = v.Y;
            m.M43 = v.Z;
        }

        public static Vector3 ToVector3(Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3 ToVector3(ref Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static float UpperLeft3x3Determinant(ref Matrix m)
        {
            float ret = ((m.M11 * m.M22 * m.M33) + (m.M12 * m.M23 * m.M31) + (m.M13 * m.M21 * m.M32)) -
                        ((m.M31 * m.M22 * m.M13) + (m.M32 * m.M23 * m.M11) + (m.M33 * m.M21 * m.M12));

            return ret;
        }


        private static void FindEigenspaceHelper(ref Matrix3 R, ref float m1, ref float m2, ref float m3, ref float m4, ref float m5, int i1, int i2)
        {
            if (!AboutZero(m2))
            {
                float u = (0.5f * (m4 - m1)) / m2;
                float u2 = u * u;
                float u2p1 = u2 + 1.0f;
                float t = !AboutEqual(u2p1, u2) ? (LessThan(u, 0.0f) ? -1.0f : 1.0f) * ((float)(Math.Sqrt(u2p1)) - Math.Abs(u)) : 0.5f / u;
                float c = 1.0f / ((float)(Math.Sqrt((t * t) + 1.0f)));
                float s = c * t;

                m1 -= t * m2;
                m4 += t * m2;
                m2 = 0.0f;

                float t1 = (c * m3) - (s * m5);
                m5 = (s * m3) + (c * m5);
                m3 = t1;

                for (int i = 0; i < 3; i++)
                {
                    float t2 = (c * R[i, i1]) - (s * R[i, i2]);
                    R[i, i2] = (s * R[i, i1]) + (c * R[i, i2]);
                    R[i, i1] = t2;
                }
            }
        }

        // From: http://www.terathon.com/code/linear.php
        public static void FindEigenspace(ref Matrix3 M, out Vector3 arValues, out Matrix3 arVectors)
        {
            float m11 = M.M11; float m12 = M.M12; float m13 = M.M13;
            float m22 = M.M22; float m23 = M.M23;
            float m33 = M.M33;

            Matrix3 R = Matrix3.Identity;

            for (int i = 0; i < kFindEigenspaceIterations; i++)
            {
                if (AboutZero(Math.Abs(m12), kJacobiToleranceFloat) &&
                    AboutZero(Math.Abs(m13), kJacobiToleranceFloat) &&
                    AboutZero(Math.Abs(m23), kJacobiToleranceFloat))
                {
                    break;
                }

                FindEigenspaceHelper(ref R, ref m11, ref m12, ref m13, ref m22, ref m23, 0, 1);
                FindEigenspaceHelper(ref R, ref m11, ref m13, ref m12, ref m33, ref m23, 0, 2);
                FindEigenspaceHelper(ref R, ref m22, ref m23, ref m12, ref m33, ref m13, 1, 2);
            }

            arValues.X = m11;
            arValues.Y = m22;
            arValues.Z = m33;

            arVectors = R;
        }

        private static Vector3[] mCornerBuffer = new Vector3[8];

        public static float Sum(Vector4 u)
        {
            return (u.X + u.Y + u.Z + u.W);
        }

        /// <summary>
        /// Transform a BoundingBox (AABB) by a matrix aTransform.
        /// </summary>
        /// <param name="aAABB">The input AABB.</param>
        /// <param name="aTransform">The transform to apply.</param>
        /// <param name="arOutAABB">The resulting AABB.</param>
        public static void Transform(ref BoundingBox aAABB, ref Matrix aTransform, out BoundingBox arOutAABB)
        {
            aAABB.GetCorners(mCornerBuffer);
            Vector3.Transform(mCornerBuffer, ref aTransform, mCornerBuffer);

            arOutAABB = BoundingBox.CreateFromPoints(mCornerBuffer);
        }

        /// <summary>
        /// Transforms a BoundingSphere by the given transform. 
        /// </summary>
        /// <remarks>
        /// Necessary because the built-in BoundingSphere.Transform() function apparently
        /// doesn't handle general SRT transforms well. I was getting too small radii
        /// or even negative radii when applying a transform with rotation or negative scaling
        /// coefficients.
        /// </remarks>
        /// <param name="aSphere">Sphere to transform.</param>
        /// <param name="aTransform">Transform to apply.</param>
        /// <param name="arOutSphere">Resulting sphere.</param>
        public static void Transform(ref BoundingSphere aSphere, ref Matrix aTransform, out BoundingSphere arOutSphere)
        {
            Vector3 x;
            Vector3 y;
            Vector3 z;
            Vector3.TransformNormal(ref kUnitX, ref aTransform, out x);
            Vector3.TransformNormal(ref kUnitY, ref aTransform, out y);
            Vector3.TransformNormal(ref kUnitZ, ref aTransform, out z);

            Vector3.Transform(ref aSphere.Center, ref aTransform, out arOutSphere.Center);
            arOutSphere.Radius = aSphere.Radius * Max(x.Length(), y.Length(), z.Length());
        }

        /// <summary>
        /// Triple product (a dot (b cross c))
        /// </summary>
        /// <param name="a">Vector a.</param>
        /// <param name="b">Vector b.</param>
        /// <param name="c">Vector c.</param>
        /// <returns>Scalar triple product of (a dot (b cross c))</returns>
        public static float Triple(Vector3 a, Vector3 b, Vector3 c)
        {
            return (a.X * ((b.Y * c.Z) - (b.Z * c.Y))) +
                   (a.Y * ((b.Z * c.X) - (b.X * c.Z))) +
                   (a.Z * ((b.X * c.Y) - (b.Y * c.X)));
        }

        /// <summary>
        /// Calculates the three primary axes of a container of points using
        /// Principal components analysis (PCA). See 
        /// http://en.wikipedia.org/wiki/Principal_components_analysis for a summary.
        /// These axes represent the orthogonal three axes of most change of the points.
        /// These will roughly correspond to the "natural" axes of a geometric figure.
        /// </summary>
        /// <param name="aPoints">A container of position vertices.</param>
        /// <param name="arAxisR">Principal R axis. Axis of most change.</param>
        /// <param name="arAxisS">Principal S axis. Axis of second most change.</param>
        /// <param name="arAxisT">Principal T axis. Axis of third most change.</param>
        /// <returns>The number of points in the container aPoints.</returns>
        public static int CalculatePrincipalComponentAxes(IEnumerable<Vector3> aPoints, out Vector3 arAxisR, out Vector3 arAxisS, out Vector3 arAxisT)
        {
            int count = 0;
            Vector3 mean = Vector3.Zero;

            foreach (Vector3 u in aPoints)
            {
                mean += u;
                count++;
            }

            float invCount = (count > 0) ? ((float)(1.0 / ((double)count))) : 0.0f;
            mean *= invCount;

            float m11 = 0.0f;
            float m22 = 0.0f;
            float m33 = 0.0f;
            float m12 = 0.0f;
            float m13 = 0.0f;
            float m23 = 0.0f;

            foreach (Vector3 u in aPoints)
            {
                Vector3 v = u - mean;
                m11 += v.X * v.X;
                m22 += v.Y * v.Y;
                m33 += v.Z * v.Z;
                m12 += v.X * v.Y;
                m13 += v.X * v.Z;
                m23 += v.Y * v.Z;
            }

            m11 *= invCount;
            m22 *= invCount;
            m33 *= invCount;
            m12 *= invCount;
            m13 *= invCount;
            m23 *= invCount;

            Matrix3 m = new Matrix3(m11, m12, m13, m12, m22, m23, m13, m23, m33);
            Vector3 eigenValues;
            Matrix3 eigenVectors;
            Utilities.FindEigenspace(ref m, out eigenValues, out eigenVectors);

            m11 = Math.Abs(eigenValues.X);
            m22 = Math.Abs(eigenValues.Y);
            m33 = Math.Abs(eigenValues.Z);

            if (Utilities.GreaterThan(m11, m22) && Utilities.GreaterThan(m11, m33))
            {
                arAxisR = new Vector3(eigenVectors.M11, eigenVectors.M21, eigenVectors.M31);

                if (Utilities.GreaterThan(m22, m33))
                {
                    arAxisS = new Vector3(eigenVectors.M12, eigenVectors.M22, eigenVectors.M32);
                    arAxisT = new Vector3(eigenVectors.M13, eigenVectors.M23, eigenVectors.M33);
                }
                else
                {
                    arAxisS = new Vector3(eigenVectors.M13, eigenVectors.M23, eigenVectors.M33);
                    arAxisT = new Vector3(eigenVectors.M12, eigenVectors.M22, eigenVectors.M32);
                }
            }
            else if (Utilities.GreaterThan(m22, m33))
            {
                arAxisR = new Vector3(eigenVectors.M12, eigenVectors.M22, eigenVectors.M32);

                if (Utilities.GreaterThan(m11, m33))
                {
                    arAxisS = new Vector3(eigenVectors.M11, eigenVectors.M21, eigenVectors.M31);
                    arAxisT = new Vector3(eigenVectors.M13, eigenVectors.M23, eigenVectors.M33);
                }
                else
                {
                    arAxisS = new Vector3(eigenVectors.M13, eigenVectors.M23, eigenVectors.M33);
                    arAxisT = new Vector3(eigenVectors.M11, eigenVectors.M21, eigenVectors.M31);
                }
            }
            else
            {
                arAxisR = new Vector3(eigenVectors.M13, eigenVectors.M23, eigenVectors.M33);

                if (Utilities.GreaterThan(m11, m22))
                {
                    arAxisS = new Vector3(eigenVectors.M11, eigenVectors.M21, eigenVectors.M31);
                    arAxisT = new Vector3(eigenVectors.M12, eigenVectors.M22, eigenVectors.M32);
                }
                else
                {
                    arAxisS = new Vector3(eigenVectors.M12, eigenVectors.M22, eigenVectors.M32);
                    arAxisT = new Vector3(eigenVectors.M11, eigenVectors.M21, eigenVectors.M31);
                }
            }

            arAxisR.Normalize();
            arAxisS.Normalize();
            arAxisT.Normalize();

            Matrix reflection = new Matrix(arAxisR.X, arAxisR.Y, arAxisR.Z, 0.0f,
                                  arAxisS.X, arAxisS.Y, arAxisS.Z, 0.0f,
                                  arAxisT.X, arAxisT.Y, arAxisT.Z, 0.0f,
                                  0, 0, 0, 1);

            if (Utilities.IsReflection(ref reflection)) { arAxisT = -arAxisT; }

            return count;
        }

        /// <summary>
        /// Given a container a points and three "natural" axes of the points, this function
        /// calculates the center of the points.
        /// </summary>
        /// <param name="aPoints">A container of points.</param>
        /// <param name="aAxisR">The axis of most change.</param>
        /// <param name="aAxisS">The second most axis of change.</param>
        /// <param name="aAxisT">The third most axis of change.</param>
        /// <param name="arCenter">The output center of the points.</param>
        public static void CalculateCenter(IEnumerable<Vector3> aPoints, ref Vector3 aAxisR, ref Vector3 aAxisS, ref Vector3 aAxisT, out Vector3 arCenter)
        {
            Vector3 min = kMaxVector3;
            Vector3 max = kMinVector3;

            foreach (Vector3 v in aPoints)
            {
                Vector3 a;
                a.X = Vector3.Dot(v, aAxisR);
                a.Y = Vector3.Dot(v, aAxisS);
                a.Z = Vector3.Dot(v, aAxisT);

                min = Vector3.Min(min, a);
                max = Vector3.Max(max, a);
            }

            Vector3 abc = 0.5f * (max + min);

            arCenter = (abc.X * aAxisR) + (abc.Y * aAxisS) + (abc.Z * aAxisT);
        }

        /// <summary>
        /// Given a three orthonormal axes and a collection of points, this function will
        /// return the center and half extents of the axes R, S, T that form a tightest fitting 
        /// OBB around the collection of points.
        /// </summary>
        /// <param name="aPoints">The collection of points.</param>
        /// <param name="arCenter">Origin of the coordinate frame formed by R, S, T.</param>
        /// <param name="aAxisR">Axis of most change.</param>
        /// <param name="aAxisS">Axis of second most change.</param>
        /// <param name="aAxisT">Axis of third most change.</param>
        /// <param name="arHalfExtents">Output half extents of R, S, T stored as X, Y, Z.</param>
        public static void CalculateCenterAndHalfExtents(IEnumerable<Vector3> aPoints, ref Vector3 aAxisR, ref Vector3 aAxisS, ref Vector3 aAxisT, out Vector3 arCenter, out Vector3 arHalfExtents)
        {
            Vector3 min = kMaxVector3;
            Vector3 max = kMinVector3;

            foreach (Vector3 v in aPoints)
            {
                Vector3 a;
                a.X = Vector3.Dot(v, aAxisR);
                a.Y = Vector3.Dot(v, aAxisS);
                a.Z = Vector3.Dot(v, aAxisT);

                min = Vector3.Min(min, a);
                max = Vector3.Max(max, a);
            }

            Vector3 abc = 0.5f * (max + min);

            arCenter = (abc.X * aAxisR) + (abc.Y * aAxisS) + (abc.Z * aAxisT);
            arHalfExtents = 0.5f * (max - min);
        }

        /// <summary>
        /// Gvien an origin and three orthonormal axes, this function will return a matrix
        /// that will transform a point into the local coordinate frame formed by the
        /// origin and axes. This function chooses the axes X, Y, Z for R, S, T such that 
        /// the overall orientation change is minimal.
        /// </summary>
        /// <param name="aCenter">Origin of the new coordinate frame.</param>
        /// <param name="aAxisR">Axis of most change.</param>
        /// <param name="aAxisS">Axis of second most change.</param>
        /// <param name="aAxisT">Axis of third most change.</param>
        /// <param name="arOut">Output transform.</param>
        public static void CalculateMatrix(ref Vector3 aCenter, ref Vector3 aAxisR, ref Vector3 aAxisS, ref Vector3 aAxisT, out Matrix arOut)
        {
            arOut = new Matrix(
                aAxisR.X, aAxisR.Y, aAxisR.Z, 0.0f,
                aAxisS.X, aAxisS.Y, aAxisS.Z, 0.0f,
                aAxisT.X, aAxisT.Y, aAxisT.Z, 0.0f,
                aCenter.X, aCenter.Y, aCenter.Z, 1.0f);
        }

        public static float Cross(Vector2 u, Vector2 v)
        {
            float ret = ((u.X * v.Y) - (u.Y * v.X));

            return ret;
        }

        public static bool IsNaN(BoundingBox bb)
        {
            return (IsNaN(bb.Min) || IsNaN(bb.Max));
        }

        public static bool IsNaN(BoundingSphere bs)
        {
            return (IsNaN(bs.Center) || float.IsNaN(bs.Radius));
        }

        public static bool IsNaN(CoordinateFrame cf)
        {
            return (IsNaN(cf.Translation) || IsNaN(cf.Orientation));
        }

        public static bool IsNaN(Matrix3 m)
        {
            return (float.IsNaN(m.M11) || float.IsNaN(m.M12) || float.IsNaN(m.M13) ||
                    float.IsNaN(m.M21) || float.IsNaN(m.M22) || float.IsNaN(m.M23) ||
                    float.IsNaN(m.M31) || float.IsNaN(m.M32) || float.IsNaN(m.M33));
        }

        public static bool IsNaN(Matrix m)
        {
            return (float.IsNaN(m.M11) || float.IsNaN(m.M12) || float.IsNaN(m.M13) || float.IsNaN(m.M14) ||
                    float.IsNaN(m.M21) || float.IsNaN(m.M22) || float.IsNaN(m.M23) || float.IsNaN(m.M24) ||
                    float.IsNaN(m.M31) || float.IsNaN(m.M32) || float.IsNaN(m.M33) || float.IsNaN(m.M34) ||
                    float.IsNaN(m.M41) || float.IsNaN(m.M42) || float.IsNaN(m.M43) || float.IsNaN(m.M44));
        }

        public static bool IsNaN(Quaternion q)
        {
            return (float.IsNaN(q.X) || float.IsNaN(q.Y) || float.IsNaN(q.Z) || float.IsNaN(q.W));
        }

        public static bool IsNaN(Vector3 u)
        {
            return (float.IsNaN(u.X) || float.IsNaN(u.Y) || float.IsNaN(u.Z));
        }

        public static bool IsNaN(Vector4 u)
        {
            return (float.IsNaN(u.X) || float.IsNaN(u.Y) || float.IsNaN(u.Z) || float.IsNaN(u.W));
        }
        #endregion

        #region Geometry
        // Sutherland-Hodgman clipping algorithm
        // http://en.wikipedia.org/wiki/Sutherland-Hodgeman
        public static int Clip(Vector3[] aVertices, SiatPlane aPlane, out Vector3[] arNewVertices)
        {
            int positive = 0;
            int negative = 0;

            int vertexCount = aVertices.Length;

            PlaneIntersectionType[] intersections = new PlaneIntersectionType[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float d = aPlane.Plane.DotCoordinate(aVertices[i]);
                if (GreaterThan(d, kLooseToleranceFloat))
                {
                    intersections[i] = PlaneIntersectionType.Front;
                    positive++;
                }
                else if (LessThan(d, kNegativeLooseToleranceFloat))
                {
                    intersections[i] = PlaneIntersectionType.Back;
                    negative++;
                }
                else
                {
                    intersections[i] = PlaneIntersectionType.Intersecting;
                }
            }

            if (negative == 0)
            {
                arNewVertices = aVertices;
                return vertexCount;
            }
            else if (positive == 0)
            {
                arNewVertices = null;
                return 0;
            }

            Vector3[] outVertices = new Vector3[vertexCount + 1];

            int count = 0;
            int Si = vertexCount - 1;

            for (int Pi = 0; Pi < vertexCount; Pi++)
            {
                PlaneIntersectionType P = intersections[Pi];
                PlaneIntersectionType S = intersections[Si];

                if (P == PlaneIntersectionType.Front)
                {
                    if (S == PlaneIntersectionType.Front)
                    {
                        outVertices[count++] = aVertices[Pi];
                    }
                    else
                    {
                        outVertices[count++] = ComputeIntersection(ref aVertices[Si], ref aVertices[Pi], ref aPlane.Plane);
                        outVertices[count++] = aVertices[Pi];
                    }
                }
                else if (S == PlaneIntersectionType.Front)
                {
                    outVertices[count++] = ComputeIntersection(ref aVertices[Pi], ref aVertices[Si], ref aPlane.Plane);
                }

                Si = Pi;
            }

            arNewVertices = new Vector3[count];
            Array.Copy(outVertices, arNewVertices, count);

            return arNewVertices.Length;
        }

        public static int Clip(Vector3[] aVertices, SiatPlane[] aPlanes, int aStartIndex, int aCount, out Vector3[] arNewVertices)
        {
            Vector3[] vertices = new Vector3[aVertices.Length];
            Array.Copy(aVertices, vertices, aVertices.Length);

            for (int i = aStartIndex; i < aCount; i++)
            {
                if (Clip(vertices, aPlanes[i], out vertices) == 0)
                {
                    arNewVertices = null;
                    return 0;
                }
            }

            arNewVertices = vertices;
            return arNewVertices.Length;
        }

        public static int Clip(Vector3[] aVertices, SiatPlane[] aPlanes, out Vector3[] arNewVertices)
        {
            return Clip(aVertices, aPlanes, 0, aPlanes.Length, out arNewVertices);
        }

        public struct Edge : IComparable<Edge>
        {
            public Edge(int aTriangle, int aVertexIndex1, int aVertexIndex2)
            {
                TriangleA = aTriangle;
                TriangleB = aTriangle;
                VertexIndex1ofA = aVertexIndex1;
                VertexIndex2ofA = aVertexIndex2;
            }

            public int TriangleA;
            public int TriangleB;

            public int VertexIndex1ofA;
            public int VertexIndex2ofA;

            public int CompareTo(Edge b)
            {
                int c = VertexIndex1ofA.CompareTo(b.VertexIndex1ofA);

                if (c == 0) { return (VertexIndex2ofA.CompareTo(b.VertexIndex2ofA)); }
                else { return c; }
            }
        }

        /// <summary>
        /// Builds an edge list for a triangle mesh.
        /// </summary>
        /// <param name="aIndices">A list of indices, expected to be a TriangleList.</param>
        /// <param name="arEdges">The resulting edge list.</param>
        /// <remarks>
        /// Based on Eric Lengyel's algorithm: http://www.terathon.com/code/edges.html
        /// 
        /// The algorithm is based on the fact that with a closed 2D manifold, when the index i1 < i2
        /// for an edge of a triangle A, the triangle B that shares that edge will have indices i1 > i2
        /// for the shared edge. 
        /// 
        /// A change to Lengyel's implementation inserts edges during the i1 > i2 pass that had no shared
        /// edge. This essentially treats the unshared line segments of non-closed manifolds as edges.
        /// </remarks>
        public static Edge[] BuildEdges(int[] aIndices)
        {
            int indexCount = aIndices.Length;
            if (indexCount % 3 != 0) { throw new Exception("Indices are not expected size."); }

            InsertionSortList<Edge> edges = new InsertionSortList<Edge>();

            #region Find edges where vi1 < vi2
            for (int i = 0; i < indexCount; i += 3)
            {
                int vi1 = aIndices[i + 2];

                for (int j = 0; j < 3; j++)
                {
                    int vi2 = aIndices[i + j];

                    if (vi1 < vi2) { edges.Add(new Edge(i, vi1, vi2)); }

                    vi1 = vi2;
                }
            }

                        #endregion

            #region Find edges where vi1 > vi2
            for (int i = 0; i < indexCount; i += 3)
            {
                int vi1 = aIndices[i + 2];

                for (int j = 0; j < 3; j++)
                {
                    int vi2 = aIndices[i + j];

                    if (vi1 > vi2)
                    {
                        int iEdge = edges.IndexOf(new Edge(i, vi2, vi1));

                        // edge with flipped indices found, this is shared edge, so indicate this triangle
                        // as triangle B.
                        if (iEdge >= 0) { edges.Data[iEdge].TriangleB = i; }
                        // otherwise this is a unique edge with no shared polygon, so insert this as a non-closed
                        // edge.
                        else { edges.Add(new Edge(i, vi1, vi2)); }
                    }

                    vi1 = vi2;
                }
            }
            #endregion

            edges.Compact();

            return edges.Data;
        }
        #endregion

        #region Geometry
        /// <summary>
        /// Gets the number of primitives a geometry bucket has given an index buffer
        /// size and a primitive type.
        /// </summary>
        /// <param name="aType">Type of the geometry bucket.</param>
        /// <param name="aIndexBufferSize">Size of the index buffer for this bucket.</param>
        /// <returns></returns>
        public static int GetPrimitiveCount(PrimitiveType aType, int aIndexBufferSize)
        {
            if (aIndexBufferSize <= 0)
            {
                return 0;
            }
            else
            {
                switch (aType)
                {
                    case PrimitiveType.LineList: return aIndexBufferSize / 2;
                    case PrimitiveType.LineStrip: return aIndexBufferSize - 1;
                    case PrimitiveType.PointList: return aIndexBufferSize;
                    case PrimitiveType.TriangleFan: return aIndexBufferSize - 2;
                    case PrimitiveType.TriangleList: return aIndexBufferSize / 3;
                    case PrimitiveType.TriangleStrip: return aIndexBufferSize - 2;
                    default:
                        throw new Exception(Utilities.kShouldNotBeHere);
                }
            }
        }

        public static BoundingSphere CalculateBounding(IList<float> vertices, VertexElement[] aVertexDeclaration, int aVertexStrideInSingles)
        {
            int elementCount = aVertexDeclaration.Length;
            int vertexCount = vertices.Count;

            int positionIndex = -1;
            for (int i = 0; i < elementCount; i++)
            {
                if (aVertexDeclaration[i].VertexElementUsage == VertexElementUsage.Position)
                {
                    positionIndex = i;
                    break;
                }
            }

            int offset = aVertexDeclaration[positionIndex].Offset / sizeof(float);

            List<Vector3> vec3 = new List<Vector3>();
            for (int i = offset; i < vertexCount; i += aVertexStrideInSingles)
            {
                vec3.Add(new Vector3(vertices[i + 0], vertices[i + 1], vertices[i + 2]));
            }

            return BoundingSphere.CreateFromPoints(vec3);
        }

        /// <summary>
        /// Inverts the v channel of any texture coordinate channels in an interleaved vertex array.
        /// </summary>
        /// <param name="aVertices">An interleaved vertex array.</param>
        /// <param name="aVertexDeclaration">Declaration describing the vertices in aVertices.</param>
        /// <param name="aVertexStrideInSingles">The stride of a single vertex element in floats.</param>
        public static void FlipTextureV(List<float> aVertices, VertexElement[] aVertexDeclaration, int aVertexStrideInSingles)
        {
            const int kVoffset = 1;

            int elementCount = aVertexDeclaration.Length;
            int vertexCount = aVertices.Count;

            for (int i = 0; i < elementCount; i++)
            {
                if (aVertexDeclaration[i].VertexElementUsage == VertexElementUsage.TextureCoordinate)
                {
                    for (int j = (int)aVertexDeclaration[i].Offset / sizeof(float); j < vertexCount; j += aVertexStrideInSingles)
                    {
                        aVertices[j + kVoffset] = 1.0f - aVertices[j + kVoffset];
                    }
                }
            }
        }

        /// <summary>
        /// Swaps the winding order of an index buffer from clockwise to counter-clockwise
        /// or vice-versa depending on the primitive type.
        /// </summary>
        /// <param name="aType">The type of the geometry this index buffer refers to.</param>
        /// <param name="arIndices">The index buffer.</param>
        public static void FlipWindingOrder(PrimitiveType aType, List<int> arIndices)
        {
            int count = arIndices.Count;
            if (aType == PrimitiveType.TriangleList)
            {
                for (int i = 1; i + 1 < count; i += 3)
                {
                    int t = arIndices[i];
                    arIndices[i] = arIndices[i + 1];
                    arIndices[i + 1] = t;
                }
            }
            else if (aType == PrimitiveType.TriangleFan)
            {
                if (count > 1)
                {
                    arIndices.Reverse(1, count - 1);
                }
            }
            else if (aType == PrimitiveType.TriangleStrip)
            {
                if (count > 2)
                {
                    int t = arIndices[1];
                    arIndices[1] = arIndices[2];
                    arIndices[2] = t;
                }
            }
        }

        public static void FlipWindingOrder(PrimitiveType aType, int[] arIndices)
        {
            int count = arIndices.Length;
            if (aType == PrimitiveType.TriangleList)
            {
                for (int i = 1; i + 1 < count; i += 3)
                {
                    int t = arIndices[i];
                    arIndices[i] = arIndices[i + 1];
                    arIndices[i + 1] = t;
                }
            }
            else if (aType == PrimitiveType.TriangleFan)
            {
                if (count > 1)
                {
                    Array.Reverse(arIndices, 1, count - 1);
                }
            }
            else if (aType == PrimitiveType.TriangleStrip)
            {
                if (count > 2)
                {
                    int t = arIndices[1];
                    arIndices[1] = arIndices[2];
                    arIndices[2] = t;
                }
            }
        }
        #endregion

        #region Stat
        public static float Mean(List<float> a)
        {
            float mean = 0.0f;

            if (a.Count == 0)
            {
                return 0;
            }

            int count = a.Count;
            for (int i = 0; i < count; i++)
            {
                mean += a[i];
            }

            mean /= ((float)a.Count);

            return mean;
        }

        public static float Skew(List<float> a, float aMean, float aStdDev)
        {
            float n = 0.0f;            
            
            if (a.Count < 2)
            {
                return float.NaN;
            }
            
            int count = a.Count;
            for (int i = 0; i < count; i++)
            {
                float t = (a[i] - aMean);
                n += (t * t * t);
            }
            
            return (n / (((float)count) - 1.0f) * aStdDev * aStdDev * aStdDev);
        }

        public static float StatNormalize(float aValue, float aMean, float aStdDev)
        {
            return (aValue - aMean) / aStdDev;
        }

        public static float StdDev(List<float> a, float aMean)
        {
            return (float)Math.Sqrt(Variance(a, aMean));
        }

        public static float Variance(List<float> a, float aMean)
        {
            float ret = 0.0f;

            if (a.Count < 2)
            {
                return 0.0f;
            }

            int count = a.Count;
            for (int i = 0; i < count; i++)
            {
                float t = (a[i] - aMean);
                ret += (t * t);
            }

            return (ret / (((float)count) - 1.0f));
        }
        #endregion
    }
}
