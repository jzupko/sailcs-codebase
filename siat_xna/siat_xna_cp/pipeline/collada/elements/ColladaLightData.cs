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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    /// <summary>
    /// Encapsulates a COLLADA light element (ambient, directional, point, or spot).
    /// </summary>
    /// <remarks>
    /// Color is currently folded into the class although it can have a sid (used for
    /// targetting animations). This should be extracted eventually.
    /// </remarks>
    public sealed class ColladaLightData : _ColladaElement
    {
        #region Private members
        private readonly Vector3 mColor;
        private readonly float mConstantAttenuation = Defaults.kConstantAttenuation;
        private readonly float mLinearAttenuation = Defaults.kLinearAttenuation;
        private readonly float mQuadraticAttenuation = Defaults.kQuadraticAttenuation;
        private readonly float mFalloffAngleDegrees = Defaults.kFalloffAngle;
        private readonly float mFalloffExponent = Defaults.kFalloffExponent;
        private readonly Enums.LightType mType = Enums.LightType.kAmbient;
        #endregion

        public const string kColorElement = "color";

        public ColladaLightData(XmlReader aReader, Enums.LightType aType)
        {
            mType = aType;

            #region Children
            _NextElement(aReader);

            #region Color
            string color;
            float[] colorValues = new float[3];
            _SetValueRequired(aReader, kColorElement, out color);
            Utilities.Tokenize(color, colorValues, XmlConvert.ToSingle);
            mColor = new Vector3(colorValues[0], colorValues[1], colorValues[2]);
            #endregion

            if (mType == Enums.LightType.kPoint || mType == Enums.LightType.kSpot)
            {
                _SetValueOptional(aReader, Elements.kConstantAttenuation.Name, ref mConstantAttenuation);
                _SetValueOptional(aReader, Elements.kLinearAttenuation.Name, ref mLinearAttenuation);
                _SetValueOptional(aReader, Elements.kQuadraticAttenuation.Name, ref mQuadraticAttenuation);
            }

            if (mType == Enums.LightType.kSpot)
            {
                _SetValueOptional(aReader, Elements.kFalloffAngle.Name, ref mFalloffAngleDegrees);
                _SetValueOptional(aReader, Elements.kFalloffExponent.Name, ref mFalloffExponent);
            }
            #endregion
        }

        public Vector3 Color { get { return mColor; } }
        public float ConstantAttenuation { get { return mConstantAttenuation; } }
        public float LinearAttenuation { get { return mLinearAttenuation; } }
        public float QuadraticAttenuation { get { return mQuadraticAttenuation; } }
        public float FalloffAngleInDegrees { get { return mFalloffAngleDegrees; } }
        public float FalloffExponent { get { return mFalloffExponent; } }
        public Enums.LightType Type { get { return mType; } }
    }
}
