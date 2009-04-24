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
    public sealed class ColladaLookAt : _ColladaTransformElement
    {
        #region Private members
        private Vector3 mEyePosition;
        private Vector3 mTargetPosition;
        private Vector3 mUpAxis;
        #endregion

        public const int kLookAtParts = 9;

        public ColladaLookAt(XmlReader aReader)
            : base(aReader)
        {
            #region Element values
            float[] buf = new float[kLookAtParts];
            string value = string.Empty;
            _SetValue(aReader, ref value);
            Utilities.Tokenize(value, buf, XmlConvert.ToSingle);

            mEyePosition = new Vector3(buf[0], buf[1], buf[2]);
            mTargetPosition = new Vector3(buf[3], buf[4], buf[5]);
            mUpAxis = new Vector3(buf[6], buf[7], buf[8]);
            #endregion
        }

        public override Matrix XnaMatrix { get { return Matrix.CreateLookAt(EyePosition, TargetPosition, UpAxis); } }
        public Vector3 EyePosition { get { return mEyePosition; } }
        public Vector3 TargetPosition { get { return mTargetPosition; } }
        public Vector3 UpAxis { get { return mUpAxis; } }
    }
}
