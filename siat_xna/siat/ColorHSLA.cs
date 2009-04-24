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
using System.Text;

namespace siat
{
    public struct ColorHSLA
    {
        #region Private members
        private static float _ToVector4Helper(float v1, float v2, float vH)
        {
            if (vH < 0.0f) { vH += 1.0f; }
            if (vH > 1.0f) { vH -= 1.0f; }
            if ((6.0f * vH) < 1.0f) { return (v1 + (v2 - v1) * 6.0f * vH); }
            if ((2.0f * vH) < 1.0f) { return (v2); }
            if ((3.0f * vH) < 2.0f) { return (v1 + (v2 - v1) * ((2.0f / 3.0f) - vH) * 6.0f); }
            
            return v1;
        }
        #endregion

        public ColorHSLA(float h, float s, float l, float a)
        {
            H = h;
            S = s;
            L = l;
            A = a;
        }

        public float H;
        public float S;
        public float L;
        public float A;

        public bool AboutEqual(ColorHSLA b)
        {
            return AboutEqual(b, Utilities.kZeroToleranceFloat);
        }

        public bool AboutEqual(ColorHSLA b, float aTolerance)
        {
            return (Utilities.AboutEqual(H, b.H, aTolerance) &&
                    Utilities.AboutEqual(S, b.S, aTolerance) &&
                    Utilities.AboutEqual(L, b.L, aTolerance) &&
                    Utilities.AboutEqual(A, b.A, aTolerance));
        }

        public static ColorHSLA From(Vector3 aColorRGB) { return From(new Vector4(aColorRGB, 1.0f)); }
        public static ColorHSLA From(Vector4 aColorRGBA)
        {
            ColorHSLA ret = new ColorHSLA();

            const float k1 = (float)(1.0 / 6.0);
            const float k2 = (float)(1.0 / 3.0);
            const float k3 = (float)(2.0 / 3.0);

            float min  = Utilities.Min(aColorRGBA.X, aColorRGBA.Y, aColorRGBA.Z);
            float max  = Utilities.Max(aColorRGBA.X, aColorRGBA.Y, aColorRGBA.Z);
            float diff = max - min;
            float sum  = max + min;

            ret.A = aColorRGBA.W;
            ret.L = sum / 2.0f;

            if (Utilities.AboutZero(ret.L) || Utilities.AboutEqual(max, min))
            {
                ret.H = 0.0f;
                ret.S = 0.0f;

                return ret;
            }
            else if (Utilities.GreaterThan(ret.L, 0.5f))
            {
                ret.S = diff / (2.0f - sum);
            }
            else
            {
                ret.S = diff / sum;
            }

            if (max == aColorRGBA.X)
            {
                ret.H = k1 * ((aColorRGBA.Y - aColorRGBA.Z) / diff);

                if (aColorRGBA.Y < aColorRGBA.Z)
                {
                    ret.H += 1.0f;
                }
            }
            else if (max == aColorRGBA.Y)
            {
                ret.H = (k1 * ((aColorRGBA.Z - aColorRGBA.X) / diff)) + k2;
            }
            else 
            {
                ret.H = (k1 * ((aColorRGBA.X - aColorRGBA.Y) / diff)) + k3;
            }

            return ret;
        }

        public static ColorHSLA Lerp(ColorHSLA a, ColorHSLA b, float aWeightOfB)
        {
            return new ColorHSLA(
                MathHelper.Lerp(a.H, b.H, aWeightOfB),
                MathHelper.Lerp(a.S, b.S, aWeightOfB),
                MathHelper.Lerp(a.L, b.L, aWeightOfB),
                MathHelper.Lerp(a.A, b.A, aWeightOfB));
        }

        public static Vector3 ToVector3(ColorHSLA aColorHSLA) { return Utilities.ToVector3(ToVector4(aColorHSLA)); }
        public static Vector4 ToVector4(ColorHSLA aColorHSLA)
        {
            const float k1 = (float)(1.0 / 3.0);

            Vector4 ret = Vector4.Zero;

            ret.W = aColorHSLA.A;

            if (Utilities.AboutZero(aColorHSLA.A))
            {
                ret.X = ret.Y = ret.Z = aColorHSLA.L;
            }
            else
            {
                float v1 = 0.0f;
                float v2 = 0.0f;

                if (aColorHSLA.L < 0.5f) { v2 = (aColorHSLA.L * (1.0f + aColorHSLA.S)); }
                else { v2 = (aColorHSLA.L + aColorHSLA.S) - (aColorHSLA.S * aColorHSLA.L); }

                v1 = (2.0f * aColorHSLA.L) - v2;

                ret.X = _ToVector4Helper(v1, v2, aColorHSLA.H + k1);
                ret.Y = _ToVector4Helper(v1, v2, aColorHSLA.H);
                ret.Z = _ToVector4Helper(v1, v2, aColorHSLA.H - k1);
            }

            return ret;
        }
    }
}
