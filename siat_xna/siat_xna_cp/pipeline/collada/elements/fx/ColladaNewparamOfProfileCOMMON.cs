
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

namespace siat.pipeline.collada.elements.fx
{
    public sealed class ColladaNewparamOfProfileCOMMON : _ColladaNewparam
    {
        public ColladaNewparamOfProfileCOMMON(XmlReader aReader)
            : base(aReader)
        {
            #region Children
            _NextElement(aReader);
            _AddOptionalChild(aReader, Elements.FX.kSemantic);

            {
                int count = 0;
                count += _AddOptionalChild(aReader, Elements.FX.kFloat);
                count += _AddOptionalChild(aReader, Elements.FX.kFloat2);
                count += _AddOptionalChild(aReader, Elements.FX.kFloat3);
                count += _AddOptionalChild(aReader, Elements.FX.kSurfaceOfProfileCOMMON);
                if (aReader.NodeType != XmlNodeType.None)
                {
                    count += _AddOptionalChild(aReader, new Elements.Element(aReader.Name, delegate(XmlReader r) { return new ColladaSamplerFX(r, Enums.GetSamplerType(aReader.Name)); }));
                }

                if (count != 1)
                {
                    throw new Exception("<new_param> expects one and only one of <float>, <float2>, <float3>, <surface>, or <sampler*> as a child element.");
                }
            }
            #endregion
        }
    }
}
