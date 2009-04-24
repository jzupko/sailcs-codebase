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
    public abstract class _ColladaInput : _ColladaElement
    {
        #region Protected members
        protected uint mSet = 0u;
        protected readonly string mSemantic = "";
        protected _ColladaElement mSource = null;
        #endregion

        public const string kSemanticVertex = "VERTEX";
        public const string kSemanticNormal = "NORMAL";
        public const string kSemanticTexcoord = "TEXCOORD";
        public const string kSemanticTexcoord0 = "TEXCOORD0";
        public const string kSemanticTexcoord1 = "TEXCOORD1";
        public const string kSemanticTexcoord2 = "TEXCOORD2";
        public const string kSemanticTexcoord3 = "TEXCOORD3";
        public const string kSemanticPosition = "POSITION";

        public _ColladaInput(XmlReader aReader)
        {
            #region Attributes
            string source;
            _SetRequiredAttribute(aReader, Attributes.kSemantic, out mSemantic);
            _SetRequiredAttribute(aReader, Attributes.kSource, out source);
            ColladaDocument.QueueIdForResolution(source, delegate(_ColladaElement aResolvedElement) { mSource = aResolvedElement; });
            #endregion
        }

        public ColladaAccessor GetAccessor()
        {
            return mSource.GetFirst<ColladaTechniqueCommonOfSource>().GetFirst<ColladaAccessor>();
        }

        public _ColladaArray<T> GetArray<T>()
        {
            return mSource.GetFirst<_ColladaArray<T>>();
        }

        public bool HasSource
        {
            get
            {
                return (mSource is ColladaSource);
            }
        }

        public string Semantic
        {
            get
            {
                return mSemantic;
            }
        }

        public _ColladaElement Source
        {
            get
            {
                return mSource;
            }
        }

        public uint Set
        {
            get
            {
                return mSet;
            }
        }

        public uint Stride
        {
            get
            {
                return mSource.GetFirst<ColladaTechniqueCommonOfSource>().GetFirst<ColladaAccessor>().Stride;
            }
        }

    }
}
