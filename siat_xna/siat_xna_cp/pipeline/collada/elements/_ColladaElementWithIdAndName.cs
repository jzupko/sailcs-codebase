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
    // Warning: make sure if you specialize this class that you call ColladaDocument.PushSidNamespace() and
    //     ColladaDocument.PopSidNamespace() around adding any children.
    public abstract class _ColladaElementWithIdAndName : _ColladaElementWithId
    {
        #region Protected members
        protected readonly string mName = "";
        #endregion

        public _ColladaElementWithIdAndName(XmlReader aReader)
            : base(aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kName, ref mName);
            #endregion
        }

        public string Name { get { return mName; } }
    }
}