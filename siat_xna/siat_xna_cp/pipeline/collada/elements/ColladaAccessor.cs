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
    public sealed class ColladaAccessor : _ColladaElement
    {
        #region Private members
        private readonly uint mCount = 0;
        private readonly uint mOffset = Defaults.kOffsetDefault;
        private _ColladaElement mSource = null;
        private readonly uint mStride = Defaults.kStrideDefault;
        #endregion

        public ColladaAccessor(XmlReader aReader)
        {
            #region Attributes
            _SetRequiredAttribute(aReader, Attributes.kCount, out mCount);
            _SetOptionalAttribute(aReader, Attributes.kOffset, ref mOffset);
            #region source
            {
                string source;
                _SetRequiredAttribute(aReader, Attributes.kSource, out source);
                ColladaDocument.QueueIdForResolution(source, delegate(_ColladaElement a) { mSource = a; });
            }
            #endregion
            _SetOptionalAttribute(aReader, Attributes.kStride, ref mStride);
            #endregion

            #region Children
            _NextElement(aReader);
            _AddZeroToManyChildren(aReader, Elements.kParam);
            #endregion
        }

        public uint Count { get { return mCount; } }
        public uint Offset { get { return mOffset; } }
        public _ColladaElement Source { get { return mSource; } }
        public uint Stride { get { return mStride; } }
    }
}
