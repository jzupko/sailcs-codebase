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
    /// <summary>
    /// Encapsulation of a COLLADA "vcount" element.
    /// </summary>
    public sealed class ColladaVcount : _ColladaElement
    {
        #region Private members
        private readonly uint[] mSides;
        private uint mExpectedPrimitivesCount = 0;

        private void _CalculateExpectedPrimitivesCount()
        {
            mExpectedPrimitivesCount = 0;
            int count = mSides.Length;

            for (int i = 0; i < count; i++)
            {
                mExpectedPrimitivesCount += mSides[i];
            }
        }
        #endregion

        public ColladaVcount(XmlReader aReader)
        {
            #region Element value
            string value = "";
            _SetValue(aReader, ref value);
            mSides = Utilities.Tokenize<uint>(value, XmlConvert.ToUInt32);
            _CalculateExpectedPrimitivesCount();
            #endregion
        }

        public ColladaVcount(XmlReader aReader, uint aExpectedSize)
        {
            #region Element value
            mSides = new uint[aExpectedSize];
            string value = "";
            _SetValue(aReader, ref value);
            Utilities.Tokenize<uint>(value, mSides, XmlConvert.ToUInt32);
            _CalculateExpectedPrimitivesCount();
            #endregion
        }

        public uint Count
        {
            get
            {
                return (uint)mSides.Length;
            }
        }

        public uint ExpectPrimitivesCount
        {
            get
            {
                return mExpectedPrimitivesCount;
            }
        }

        public uint this[uint i]
        {
            get
            {
                return mSides[i];
            }
        }
    }
}