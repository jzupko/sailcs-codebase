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
    public sealed class ColladaVertexWeights : _ColladaElement
    {
        #region Private members
        private readonly uint mCount;

        private void _VerifyInputChildren()
        {
            for (_ColladaElement e = mFirstChild; e != null; e = e.NextSibling)
            {
                ColladaInputGroupB input = e as ColladaInputGroupB;

                if (input != null)
                {
                    if (input.Semantic == Enums.InputSemantic.kJoint)
                    {
                        return;
                    }
                }
            }

            throw new Exception("<vertex_weights> requires an <input> child with " +
                "semantic \"" + Enums.InputSemantic.kJoint + "\".");
        }
        #endregion

        public ColladaVertexWeights(XmlReader aReader)
        {
            #region Attributes
            _SetRequiredAttribute(aReader, Attributes.kCount, out mCount);
            #endregion

            #region Children
            _NextElement(aReader);
            _AddRequiredChild(aReader, Elements.kInputGroupB);
            _AddOneToManyChildren(aReader, Elements.kInputGroupB);
            _VerifyInputChildren();

            #region <vcount> and <v>
            {
                Elements.Element e = new Elements.Element(Elements.kVcount.Name, delegate(XmlReader a) { return new ColladaVcount(a, mCount); });
                if (_AddOptionalChild(aReader, e) > 0)
                {
                    // Note: two entries per bone.
                    uint expectedVsize = ((ColladaVcount)mLastChild).ExpectPrimitivesCount * 2u;
                    e = new Elements.Element(Elements.kV.Name, delegate(XmlReader a) { return new ColladaPrimitives(a, expectedVsize); });
                    _AddRequiredChild(aReader, e);
                }
                else
                {
                    if (aReader.Name == Elements.kV.Name)
                    {
                        throw new Exception("<v> of <vertex_weights> must exist with <vcount>");
                    }
                }
            }
            #endregion

            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public uint Count { get { return mCount; } }
    }
}
