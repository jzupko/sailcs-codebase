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
using System.Collections.Generic;
using System.Xml;

namespace siat.pipeline.collada.elements
{
    public abstract class _ColladaPrimitive : _ColladaElement
    {
        #region Protected members
        protected readonly string mName = "";
        protected readonly uint mCount = 0;
        protected readonly string mMaterial = "";

        protected abstract void _ProcessPrimitives(int aInputCount, XmlReader aReader);
        #endregion

        public _ColladaPrimitive(XmlReader aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kName, ref mName);
            _SetRequiredAttribute(aReader, Attributes.kCount, out mCount);
            _SetOptionalAttribute(aReader, Attributes.kMaterial, ref mMaterial);
            #endregion

            #region Children
            _NextElement(aReader);
            int inputCount = _AddZeroToManyChildren(aReader, Elements.kInputGroupB);
            _ProcessPrimitives(inputCount, aReader);
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion 
  
            #region Validation
            {
                ColladaPrimitives colladaIndexBuffer = GetFirst<ColladaPrimitives>();
                uint offsetCount = OffsetCount;
                uint primitivesCount = colladaIndexBuffer.Count;
                if (offsetCount == 0u)
                {
                    throw new Exception("A COLLADA primitives element has no indices. This is not valid.");
                }

                if (primitivesCount % offsetCount != 0u)
                {
                    throw new Exception("A COLLADA primitives element has mismatched indices and data buffers.");
                }
            }
            #endregion
        }

        public uint Count
        {
            get
            {
                return mCount;
            }
        }

        public ColladaPrimitives Indices
        {
            get
            {
                return GetFirst<ColladaPrimitives>();
            }
        }

        public uint OffsetCount
        {
            get
            {
                uint ret = 0;
                foreach (ColladaInputGroupB e in GetEnumerable<ColladaInputGroupB>())
                {
                    ret = Math.Max(ret, e.Offset + 1u);
                }

                return ret;
            }
        }

        public string Material
        {
            get
            {
                return mMaterial;
            }
        }

        public _ColladaInput[] FindInputs(uint aOffset)
        {
            List<_ColladaInput> ret = new List<_ColladaInput>();

            foreach (ColladaInputGroupB b in GetEnumerable<ColladaInputGroupB>())
            {
                if (b.Semantic == Enums.InputSemantic.kVertex && b.Offset == aOffset)
                {
                    foreach (ColladaInputGroupA a in b.Source.GetEnumerable<ColladaInputGroupA>())
                    {
                        ret.Add(a);
                    }
                }
                else if (b.Offset == aOffset)
                {
                    ret.Add(b);
                }
            }

            return ret.ToArray();
        }

        public _ColladaInput FindInput(string aInputSemantic)
        {
            foreach (ColladaInputGroupB b in GetEnumerable<ColladaInputGroupB>())
            {
                if (aInputSemantic != Enums.InputSemantic.kVertex && b.Semantic == Enums.InputSemantic.kVertex)
                {
                    foreach (ColladaInputGroupA a in b.Source.GetEnumerable<ColladaInputGroupA>())
                    {
                        if (a.Semantic == aInputSemantic)
                        {
                            return a;
                        }
                    }
                }
                else if (b.Semantic == aInputSemantic)
                {
                    return b;
                }
            }

            throw new Exception("<input> with semantic \"" + aInputSemantic + "\" not found.");
        }

    }
}
