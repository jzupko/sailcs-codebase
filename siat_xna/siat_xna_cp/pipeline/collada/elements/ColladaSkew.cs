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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    public sealed class ColladaSkew : _ColladaTransformElement
    {
        #region Private members
        private float mAngleInDegrees = 0.0f;
        private Vector3 mAxisOfRotation;
        private Vector3 mAxisOfDisplacement;
        #endregion

        public const int kSkewParts = 7;

        // Note: need to clarify what the convention of skew is.
        public ColladaSkew(XmlReader aReader)
            : base(aReader)
        {
            #region Element values
            float[] buf = new float[kSkewParts];
            string value = "";
            _SetValue(aReader, ref value);
            Utilities.Tokenize(value, buf, XmlConvert.ToSingle);

            mAngleInDegrees = buf[0];
            mAxisOfRotation = new Vector3(buf[1], buf[2], buf[3]);
            mAxisOfDisplacement = new Vector3(buf[4], buf[5], buf[6]);
            #endregion
        }

        public float AngleInDegrees { get { return mAngleInDegrees; } }
        public Vector3 AxisOfRotation { get { return mAxisOfRotation; } }
        public Vector3 AxisOfDisplacement { get { return mAxisOfDisplacement; } }

        /// <summary>
        /// Calculates skew matrix - see RenderMan spec 3.2.1, page 58, https://renderman.pixar.com/products/rispec/rispec_pdf/RISpec3_2.pdf 
        /// </summary>
        /// 
        /// \todo Calculation of skew matrix needs to be tested.
        public override Matrix XnaMatrix
        {
            get
            {
                float theta = MathHelper.ToRadians(AngleInDegrees);
                float dotAngle = (float)Math.Acos(theta);
                float dotAxes = Vector3.Dot(AxisOfDisplacement, AxisOfRotation);

                if (!Utilities.GreaterThan(dotAngle, dotAxes))
                {
                    throw new Exception("Skew angle is invalid. It cannot be greater than " +
                        "or equal to the angle between the axis of displacement and the axis " +
                        " of rotation.");
                }

                Vector3 n2 = Vector3.Normalize(AxisOfRotation);
                Vector3 a = n2 * Vector3.Dot(AxisOfDisplacement, n2);
                Vector3 b = AxisOfDisplacement - a;
                Vector3 n1 = Vector3.Normalize(b);

                float s1 = Vector3.Dot(AxisOfDisplacement, n1);
                float s2 = Vector3.Dot(AxisOfDisplacement, n2);
                double rx = (s1 * Math.Cos(theta)) - (s2 * Math.Sin(theta));
                double ry = (s1 * Math.Sin(theta)) + (s2 * Math.Cos(theta));

                float alpha = 0.0f;
                Matrix ret = Matrix.Identity;

                if (!Utilities.AboutZero(s1))
                {
                    alpha = (float)((ry / rx) - (s2 / s1));
                }

                ret.M11 = (n1.X * n2.X * alpha) + 1.0f; ret.M12 = (n1.Y * n2.X * alpha); ret.M13 = (n1.Z * n2.X * alpha);
                ret.M21 = (n1.X * n2.Y * alpha); ret.M22 = (n1.Y * n2.Y * alpha) + 1.0f; ret.M23 = (n1.Z * n2.Y * alpha);
                ret.M31 = (n1.X * n2.Z * alpha); ret.M32 = (n1.Y * n2.Z * alpha); ret.M33 = (n1.Z * n2.Z * alpha) + 1.0f;

                return ret;
            }
        }
    }
}
