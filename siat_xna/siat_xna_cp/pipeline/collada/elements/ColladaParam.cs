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
    public sealed class ColladaParam : _ColladaElementWithSid
    {
        #region Private members
        private readonly Enums.ParamName mName = Defaults.kParamName;
        private readonly Enums.Type mType;
        private readonly string mSemantic = Defaults.kParamSemantic;
        #endregion

        public ColladaParam(XmlReader aReader)
            : base(aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kName, ref mName, Enums.GetParamName);
            _SetRequiredAttribute(aReader, Attributes.kType, out mType, Enums.GetType);
            _SetOptionalAttribute(aReader, Attributes.kSemantic, ref mSemantic);

            _NextElement(aReader);
            #endregion
        }

        public Enums.ParamName Name { get { return mName; } }
        public string Semantic { get { return mSemantic; }}
        public Enums.Type Type { get { return mType; } }
    }
}
