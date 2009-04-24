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
    public sealed class ColladaJoints : _ColladaElement
    {
        #region Private members
        // Todo: the input with JOINT semantic should reference a Name_array.
        //    need to find a way to verify this (the source may not exist at this point
        //    and the check needs to be deferred until it does).
        private void _VerifyInputChildren()
        {
            for (_ColladaElement e = mFirstChild; e != null; e = e.NextSibling)
            {
                ColladaInputGroupA input = e as ColladaInputGroupA;

                if (input != null)
                {
                    if (input.Semantic == Enums.InputSemantic.kJoint)
                    {
                        return;
                    }
                }
            }

            throw new Exception("<joints> requires an <input> child with semantic + \"" + Enums.InputSemantic.kJoint + "\".");
        }
        #endregion

        public ColladaJoints(XmlReader aReader)
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
