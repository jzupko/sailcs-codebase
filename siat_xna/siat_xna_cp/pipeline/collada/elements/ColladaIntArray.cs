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
    public sealed class ColladaIntArray : _ColladaArray<int>
    {
        #region Protected members
        private readonly int mMin = Defaults.kIntMinAttribute;
        private readonly int mMax = Defaults.kIntMaxAttribute;
        #endregion

        public ColladaIntArray(XmlReader aReader)
            : base(aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kMinInclusive, ref mMin);
            _SetOptionalAttribute(aReader, Attributes.kMaxInclusive, ref mMax);
            #endregion        

            #region Element value
            string value = string.Empty;
            _SetValue(aReader, ref value);
            Utilities.Tokenize<int>(value, mArray, XmlConvert.ToInt32);
            #endregion
        }
    }
}
