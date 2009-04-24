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
    public sealed class ColladaPolylist : _ColladaPrimitive
    {
        #region Protected members
        protected override void _ProcessPrimitives(int aInputCount, XmlReader aReader)
        {
            if (mCount > 0)
            {
                if (aReader.Name != Elements.kVcount.Name || aInputCount == 0)
                {
                    throw new Exception("polylist count > 0 but input or vcount data is not correct for this.");
                }
                else
                {
                    Elements.Element e = new Elements.Element(Elements.kVcount.Name, delegate(XmlReader a) { return new ColladaVcount(a, mCount); });
                    _AddRequiredChild(aReader, e);

                    uint expectedPsize = ((ColladaVcount)mLastChild).ExpectPrimitivesCount * (uint)aInputCount;
                    e = new Elements.Element(Elements.kPrimitives.Name, delegate(XmlReader a) { return new ColladaPrimitives(a, expectedPsize); });
                    _AddRequiredChild(aReader, e);
                }
            }
        }
        #endregion

        public ColladaPolylist(XmlReader aReader)
            : base(aReader)
        { }
    }
}
