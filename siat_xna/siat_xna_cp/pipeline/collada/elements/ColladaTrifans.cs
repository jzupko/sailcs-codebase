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
    public sealed class ColladaTrifans : _ColladaPrimitive
    {
        #region Protected members
        protected override void _ProcessPrimitives(int aInputCount, XmlReader aReader)
        {
            int primitiveCount = _AddZeroToManyChildren(aReader, Elements.kPrimitives);

            // Note: in the context of Trifans, mCount is apparently the number of primitives,
            //   not (apparently, the spec is vague) the number of segments like in Lines or the
            //   number of triangles like in Triangles
            if (primitiveCount != mCount)
            {
                throw new Exception("count is not equal to primitive count.");
            }
        }
        #endregion

        public ColladaTrifans(XmlReader aReader)
            : base(aReader)
        { }
    }
}
