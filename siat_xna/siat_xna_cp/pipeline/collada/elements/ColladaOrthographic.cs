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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    // Note: this is one of the few times that I am not extracting information from
    //     the COLLADA file. Each of the child elements of an Orthographi element
    //     <xmag>, <ymag>, <aspect_ratio>, <znear>, <zfar> can have an individual sid.
    //     These are not stored, nor are all of the original values (whatever combination
    //     of <xmag>, <ymag>, <aspect_ratio> are convereted into width, height.
    // Todo: verify that the calculations for the case of only <xmag> or only <ymag> are
    //     correct.
    public sealed class ColladaOrthographic : _ColladaElement
    {
        #region Private members
        private readonly float mWidth;
        private readonly float mHeight;
        private readonly float mNear;
        private readonly float mFar;
        #endregion

        public ColladaOrthographic(XmlReader aReader)
        {
            #region Children
            bool bXmag = false;
            bool bYmag = false;
            bool bAspectRatio = false;
            float xmag = 0.0f;
            float ymag = 0.0f;
            float aspectRatio = 0.0f;

            _NextElement(aReader);

            bXmag = _SetValueOptional(aReader, Elements.kXmag.Name, ref xmag);
            bYmag = _SetValueOptional(aReader, Elements.kYmag.Name, ref ymag);
            bAspectRatio = _SetValueOptional(aReader, Elements.kAspectRatio.Name, ref aspectRatio);
            _SetValueRequired(aReader, Elements.kZnear.Name, out mNear);
            _SetValueRequired(aReader, Elements.kZfar.Name, out mFar);

            if (bXmag && !bYmag && !bAspectRatio)
            {
                mWidth = xmag * 2.0f;
                mHeight = xmag * 2.0f;
            }
            else if (!bXmag && bYmag && !bAspectRatio)
            {
                mWidth = ymag * 2.0f;
                mHeight = ymag * 2.0f;
            }
            else if (bXmag && bYmag && !bAspectRatio)
            {
                mWidth = xmag * 2.0f;
                mHeight = ymag * 2.0f;
            }
            else if (bXmag && !bYmag && bAspectRatio)
            {
                mWidth = xmag * 2.0f;
                mHeight = (xmag / aspectRatio) * 2.0f;
            }
            else if (!bXmag && bYmag && bAspectRatio)
            {
                mWidth = (ymag * aspectRatio) * 2.0f;
                mHeight = ymag * 2.0f;
            }
            else
            {
                throw new Exception("incorrect combination of child elements for <orthographi>.");
            }
            #endregion
        }
    }
}
