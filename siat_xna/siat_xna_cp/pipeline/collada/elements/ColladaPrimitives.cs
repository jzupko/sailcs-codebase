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
    public sealed class ColladaPrimitives : _ColladaElement
    {
        #region Private members
        private readonly int[] mIndices;
        #endregion

        public ColladaPrimitives(XmlReader aReader)
        {
            #region Element value
            string value = "";
            _SetValue(aReader, ref value);
            mIndices = Utilities.Tokenize<int>(value, XmlConvert.ToInt32);
            #endregion
        }

        public ColladaPrimitives(XmlReader aReader, uint aExpectedSize)
        {
            #region Element value
            mIndices = new int[aExpectedSize];
            string value = "";
            _SetValue(aReader, ref value);
            Utilities.Tokenize<int>(value, mIndices, XmlConvert.ToInt32);
            #endregion
        }

        public int[] GetSparse(uint aOffset, uint aStride)
        {
            uint count = (uint)mIndices.Length;
            if (count % aStride != 0) { throw new Exception("Invalid stride."); }

            int[] ret = new int[count / aStride];
            
            uint index = 0;
            for (uint i = aOffset; i < count; i += aStride)
            {
                ret[index++] = mIndices[i];
            }

            return ret;
        }

        public uint Count { get { return (uint)mIndices.Length; } }
        public int this[uint i] { get { return mIndices[i]; } }
    }
}