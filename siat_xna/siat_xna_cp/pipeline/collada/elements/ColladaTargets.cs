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
    public sealed class ColladaTargets : _ColladaElement
    {
        #region Private members
        private void _VerifyInputChildren()
        {
            bool bA = false;
            bool bB = false;

            for (_ColladaElement e = mFirstChild; e != null; e = e.NextSibling)
            {
                ColladaInputGroupA input = e as ColladaInputGroupA;

                if (input != null)
                {
                    if (input.Semantic == Enums.InputSemantic.kMorphTarget)
                    {
                        bA = true;
                    }
                    else if (input.Semantic == Enums.InputSemantic.kMorphWeight)
                    {
                        bB = true;
                    }

                    if (bA && bB)
                    {
                        return;
                    }
                }
            }

            throw new Exception("missing input semantics for <targets>.");
        }
        #endregion

        public ColladaTargets(XmlReader aReader)
        {
            #region Children
            _NextElement(aReader);
            _AddRequiredChild(aReader, Elements.kInputGroupA);
            _AddOneToManyChildren(aReader, Elements.kInputGroupA);
            _VerifyInputChildren();
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion   
        }
    }
}
