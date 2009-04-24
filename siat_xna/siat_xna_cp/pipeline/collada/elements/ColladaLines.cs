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
    /// Encapsulates a COLLADA "lines" element.
    /// </summary>
    /// <remarks>
    /// The specification is unclear regarding the lines primitive definition. The "count" attribute
    /// is inconsistent across primitives such as "lines" or "linestrips". The validity check may
    /// fail because of this.
    /// </remarks>
    public sealed class ColladaLines : _ColladaPrimitive
    {
        #region Protected members
        protected override void _ProcessPrimitives(int aInputCount, XmlReader aReader)
        {
            if (mCount > 0)
            {
                if (aReader.Name != Elements.kPrimitives.Name || aInputCount == 0)
                {
                    throw new Exception("line count > 0 but input or primitive data is not correct for this.");
                }
                else
                {
                    // number of lines * 2 vertices per line * the number of offsets (one index entry for each offset).
                    uint size = mCount * 2u * OffsetCount;

                    Elements.Element e = new Elements.Element(Elements.kPrimitives.Name, delegate(XmlReader a) { return new ColladaPrimitives(a, size); });

                    _AddRequiredChild(aReader, e);
                }
            }
        }
        #endregion

        public ColladaLines(XmlReader aReader)
            : base(aReader)
        { }
    }
}
