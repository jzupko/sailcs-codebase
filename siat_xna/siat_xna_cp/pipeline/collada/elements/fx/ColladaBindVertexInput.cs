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

namespace siat.pipeline.collada.elements.fx
{
    public sealed class ColladaBindVertexInput : _ColladaElement
    {
        #region Private members
        private readonly string mSemantic;
        private readonly string mInputSemantic;
        private readonly uint mInputSet = 0;
        #endregion

        public ColladaBindVertexInput(XmlReader aReader)
        {
            #region Attributes
            _SetRequiredAttribute(aReader, Attributes.kSemantic, out mSemantic);
            _SetRequiredAttribute(aReader, Attributes.kInputSemantic, out mInputSemantic);
            _SetOptionalAttribute(aReader, Attributes.kInputSet, ref mInputSet);
            #endregion
            _NextElement(aReader);
        }

        public string Semantic { get { return mSemantic; } }
        public string InputSemantic { get { return mInputSemantic; } }
        public uint InputSet { get { return mInputSet; } }
    }
}
