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
    public sealed class ColladaMorph : _ColladaElement
    {
        #region Private members
        // Todo: determine what the type of this should be - may need to create a common subclass for mesh, convexmesh, and spline
        private _ColladaElement mSource;
        private readonly Enums.MorphMethodType mType = Defaults.kMorphMethodAttribute;
        #endregion

        public ColladaMorph(XmlReader aReader)
        {
            #region Attributes
            string source;
            _SetRequiredAttribute(aReader, Attributes.kSource, out source);
            ColladaDocument.QueueIdForResolution(source, delegate(_ColladaElement aResolvedSource) { mSource = aResolvedSource; });

            string method = default(string);
            if (_SetOptionalAttribute(aReader, Attributes.kMethod, ref method))
            {
                mType = Enums.GetMorphMethodType(method);
            }
            #endregion

            #region Children
            _NextElement(aReader);
            _AddRequiredChild(aReader, Elements.kSource);
            _AddOneToManyChildren(aReader, Elements.kSource);
            _AddRequiredChild(aReader, Elements.kTargets);
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public _ColladaElement Source { get { return mSource; } }
        public Enums.MorphMethodType Type { get { return mType; } }
    }
}
