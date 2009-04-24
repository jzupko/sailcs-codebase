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

namespace siat
{

    public struct Degree : IComparable<Degree>
    {
        #region Private members
        private float mValue;
        #endregion

        public static readonly Degree kZero = new Degree(0.0f);
        public static readonly Degree k45 = new Degree(45.0f);
        public static readonly Degree k90 = new Degree(90.0f);
        public static readonly Degree k135 = new Degree(135.0f);
        public static readonly Degree k180 = new Degree(180.0f);
        public static readonly Degree k360 = new Degree(360.0f);

        public Degree(float a) { mValue = a; }
        public Degree(Radian r) { mValue = MathHelper.ToDegrees(r.Value); }

        public override int GetHashCode() { return mValue.GetHashCode(); }
        public float Cos() { return (float)Math.Cos(MathHelper.ToRadians(mValue)); }
        public float Cot() { return 1.0f / (float)Math.Tan(MathHelper.ToRadians(mValue)); }
        public float Sin() { return (float)Math.Sin(MathHelper.ToRadians(mValue)); }
        public float Tan() { return (float)Math.Tan(MathHelper.ToRadians(mValue)); }
        public Radian ToRadians() { return new Radian(this); }
        public float Value { get { return mValue; } }

        public static bool operator ==(Degree a, Degree b) { return (a.mValue == b.mValue); }
        public static bool operator !=(Degree a, Degree b) { return (a.mValue != b.mValue); }
        public static Degree operator -(Degree d) { return new Degree(-d.mValue); }
        public static Degree operator -(Degree a, Degree b) { return new Degree(a.mValue - b.mValue); }
        public static Degree operator *(float s, Degree a) { return new Degree(s * a.mValue); }
        public static Degree operator *(Degree a, float s) { return new Degree(a.mValue * s); }
        public static Degree operator *(Degree a, Degree b) { return new Degree(a.mValue * b.mValue); }
        public static Degree operator /(Degree a, float s) { return new Degree(a.mValue / s); }
        public static Degree operator /(Degree a, Degree b) { return new Degree(a.mValue / b.mValue); }
        public static Degree operator +(Degree a, Degree b) { return new Degree(a.mValue + b.mValue); }
        public static bool operator >=(Degree a, Degree b) { return a.mValue >= b.mValue; }
        public static bool operator <=(Degree a, Degree b) { return a.mValue <= b.mValue; }
        public static bool operator >(Degree a, Degree b) { return a.mValue > b.mValue; }
        public static bool operator <(Degree a, Degree b) { return a.mValue < b.mValue; }
        public static bool operator >(Degree a, float b) { return a.mValue > b; }
        public static bool operator >(float a, Degree b) { return a > b.mValue; }
        public static bool operator <(Degree a, float b) { return a.mValue < b; }
        public static bool operator <(float a, Degree b) { return a < b.mValue; }

        public int CompareTo(Degree b)
        {
            if (this > b) { return 1; }
            else if (this < b) { return -1; }
            else { return 0; }
        }

        public override bool Equals(object obj)
        {
            if (obj is Degree)
            {
                return (mValue == ((Degree)obj).mValue);
            }

            return false;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }
    }

    public struct Radian : IComparable<Radian>
    {
        #region Private members
        private float mValue;
        #endregion

        public static readonly Radian kZero = new Radian(0.0f);
        public static readonly Radian kPi = new Radian(MathHelper.Pi);
        public static readonly Radian kPiOver2 = new Radian(MathHelper.PiOver2);
        public static readonly Radian kPiOver4 = new Radian(MathHelper.PiOver4);
        public static readonly Radian kTwoPi = new Radian(MathHelper.TwoPi);

        public static float Cos(Radian r) { return (float)Math.Cos(r.mValue); }
        public static float Cot(Radian r) { return 1.0f / (float)Math.Tan(r.mValue); }
        public static float Sin(Radian r) { return (float)Math.Sin(r.mValue); }
        public static float Tan(Radian r) { return (float)Math.Tan(r.mValue); }

        public Radian(float a) { mValue = a; }
        public Radian(Degree d) { mValue = MathHelper.ToRadians(d.Value); }

        public override int GetHashCode() { return mValue.GetHashCode(); }
        public Degree ToDegrees() { return new Degree(this); }
        public float Value { get { return mValue; } }

        public static bool operator ==(Radian a, Radian b) { return (a.mValue == b.mValue); }
        public static bool operator !=(Radian a, Radian b) { return (a.mValue != b.mValue); }
        public static Radian operator -(Radian d) { return new Radian(-d.mValue); }
        public static Radian operator -(Radian a, Radian b) { return new Radian(a.mValue - b.mValue); }
        public static Radian operator -(float a, Radian b) { return new Radian(a - b.mValue); }
        public static Radian operator -(Radian a, float b) { return new Radian(a.mValue - b); }
        public static Vector3 operator *(Vector3 v, Radian a) { return new Vector3(v.X * a.mValue, v.Y * a.mValue, v.Z * a.mValue); }
        public static Vector3 operator *(Radian a, Vector3 v) { return new Vector3(v.X * a.mValue, v.Y * a.mValue, v.Z * a.mValue); }
        public static Radian operator *(float s, Radian a) { return new Radian(s * a.mValue); }
        public static Radian operator *(Radian a, float s) { return new Radian(a.mValue * s); }
        public static Radian operator *(Radian a, Radian b) { return new Radian(a.mValue * b.mValue); }
        public static Radian operator /(Radian a, Radian b) { return new Radian(a.mValue / b.mValue); }
        public static Radian operator /(Radian a, float s) { return new Radian(a.mValue / s); }
        public static Radian operator /(float s, Radian a) { return new Radian(s / a.mValue); }
        public static Radian operator +(Radian a, Radian b) { return new Radian(a.mValue + b.mValue); }
        public static Radian operator +(float a, Radian b) { return new Radian(a + b.mValue); }
        public static Radian operator +(Radian a, float b) { return new Radian(a.mValue + b); }
        public static bool operator >(Radian a, Radian b) { return a.mValue > b.mValue; }
        public static bool operator <(Radian a, Radian b) { return a.mValue < b.mValue; }
        public static bool operator >(Radian a, float b) { return a.mValue > b; }
        public static bool operator >(float a, Radian b) { return a > b.mValue; }
        public static bool operator <(Radian a, float b) { return a.mValue < b; }
        public static bool operator <(float a, Radian b) { return a < b.mValue; }

        public int CompareTo(Radian b)
        {
            if (this > b) { return 1; }
            else if (this < b) { return -1; }
            else { return 0; }
        }

        public override bool Equals(object obj)
        {
            if (obj is Radian)
            {
                return (mValue == ((Radian)obj).mValue);
            }

            return false;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }

        public static Radian Lerp(Radian a, Radian b, float aWeightOfB)
        {
            float av = a.Value;
            float bv = b.Value;

            while (av > bv + MathHelper.Pi) { av -= MathHelper.TwoPi; }
            while (bv > av + MathHelper.Pi) { bv -= MathHelper.TwoPi; }

            return new Radian(MathHelper.Lerp(av, bv, aWeightOfB));
        }
    }

}
