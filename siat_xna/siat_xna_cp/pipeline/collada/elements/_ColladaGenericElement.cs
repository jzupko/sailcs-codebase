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
    public sealed class _ColladaGenericElement : _ColladaElement
    {
        #region Private members
        private readonly Dictionary<string, string> mAttributes = new Dictionary<string, string>();
        private readonly string mName;
        private readonly string mValue = "";
        #endregion

        public _ColladaGenericElement(XmlReader aReader)
        {
            mName = aReader.Name;

            #region Attributes
            if (aReader.HasAttributes)
            {
                while (aReader.MoveToNextAttribute())
                {
                    mAttributes.Add(aReader.Name, aReader.Value);
                }

                aReader.MoveToElement();
            }
            #endregion

            #region Element value
            _NextElementOrText(aReader);
            if (aReader.HasValue)
            {
                mValue = aReader.Value;
                _NextElement(aReader);
            }
            #endregion

            #region Children
            while (aReader.NodeType != XmlNodeType.None)
            {
                Elements.Element e = new Elements.Element(aReader.Name, delegate(XmlReader a) { return new _ColladaGenericElement(a); });
                _AddRequiredChild(aReader, e);
            }
            #endregion
        }

        public bool GetContains(string aAttribute) { return mAttributes.ContainsKey(aAttribute); }

        public string Name { get { return mName; } }
        public string Value { get { return mValue; } }

        public string this[string s]
        {
            get
            {
                if (!mAttributes.ContainsKey(s))
                {
                    throw new Exception("Collada generic element \"" + mName + "\" does not contain " +
                        "requested attribute \"" + s + "\".");
                }

                return mAttributes[s];
            }
        }
    }
}
