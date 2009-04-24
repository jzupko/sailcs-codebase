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

namespace siat.render
{
    public enum LightType
    {
        Directional,
        Point,
        Spot
    }

    // Joe: the instanceable light data used by LightNode. I currently do not take advantage
    //      of this instancing but may in the future.
    public sealed class Light
    {
        #region Private members
        private float mFalloffCosHalfAngle;
        private float mFalloffAngleInRadians;
        private float mFalloffExponent;
        #endregion

        public Light()
        {
            FalloffAngleInRadians = MathHelper.PiOver2;
            FalloffExponent = 1.0f;
            LightAttenuation = Vector3.UnitX;
            LightDiffuse = Vector3.Zero;
            LightSpecular = Vector3.Zero;
            Type = LightType.Point;
        }

        public Light(
            float aFalloffAngleInRadians,
            float aFalloffExponent,
            Vector3 aLightAttenuation,
            Vector3 aLightDiffuse,
            Vector3 aLightSpecular,
            LightType aType)
        {
            FalloffAngleInRadians = aFalloffAngleInRadians;
            FalloffExponent = aFalloffExponent;
            LightAttenuation = aLightAttenuation;
            LightDiffuse = aLightDiffuse;
            LightSpecular = aLightSpecular;
            Type = aType;
        }

        public float FalloffAngleInRadians
        {
            get
            {
                return mFalloffAngleInRadians;
            }

            set
            {
                while (mFalloffAngleInRadians < 0.0f) { mFalloffAngleInRadians += MathHelper.TwoPi; }
                while (mFalloffAngleInRadians > MathHelper.TwoPi) { mFalloffAngleInRadians -= MathHelper.TwoPi; }

                mFalloffAngleInRadians = MathHelper.Clamp(value, 0.0f, MathHelper.PiOver2);
                mFalloffCosHalfAngle = (float)Math.Cos(mFalloffAngleInRadians * 0.5f);
            }
        }

        public float FalloffCosHalfAngle
        {
            get
            {
                return mFalloffCosHalfAngle;
            }
        }

        public float FalloffExponent
        {
            get
            {
                return mFalloffExponent;
            }

            set
            {
                mFalloffExponent = MathHelper.Clamp(value, 0.0f, 128.0f);
            }
        }

        public Vector3 LightAttenuation;
        public Vector3 LightDiffuse;
        public Vector3 LightSpecular;
        public LightType Type;
    }
}
