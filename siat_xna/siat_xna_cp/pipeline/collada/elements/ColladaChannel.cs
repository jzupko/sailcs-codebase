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
    public sealed class ColladaChannel : _ColladaElement
    {
        #region Private members
        private _ColladaElement mSource;
        private _ColladaTarget mTarget;

        private _ColladaArray<T> _GetArrayHelper<T>(string aSemantic)
        {
            return mSource.GetFirst<ColladaInputGroupA>(
                delegate(ColladaInputGroupA e)
                {
                    if (e.Semantic == aSemantic)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }).GetArray<T>();
        }
        #endregion

        public ColladaChannel(XmlReader aReader)
        {
            #region Attributes
            string source;
            _SetRequiredAttribute(aReader, Attributes.kSource, out source);
            ColladaDocument.QueueIdForResolution(source, delegate(_ColladaElement aResolved) { mSource = aResolved; });
            #region target
            {
                string target;
                _SetRequiredAttribute(aReader, Attributes.kTarget, out target);
                {
                    string targetElement;
                    string accessors;
                    _ParseTargetToSidReference(target, out targetElement, out accessors);
                    ColladaDocument.QueueSidForResolution(targetElement,
                        delegate(_ColladaElement a)
                        {
                            mTarget = new _ColladaTarget(a, accessors);
                            if (!(mTarget.Target is _ColladaChannelTarget))
                            {
                                throw new Exception("target of <channel> element is not valid.");
                            }
                            else
                            {
                                ((_ColladaChannelTarget)mTarget.Target).AddTargetOf(this);
                            }
                        });
                }
            }
            #endregion
            #endregion

            _NextElement(aReader);
        }

        public _ColladaElement Source { get { return mSource; } }
        public _ColladaTarget Target { get { return mTarget; } }
        
        public _ColladaArray<float> Inputs { get { return _GetArrayHelper<float>(Enums.InputSemantic.kInput); } }
        public _ColladaArray<float> Outputs { get { return _GetArrayHelper<float>(Enums.InputSemantic.kOutput); } }
        public _ColladaArray<float> InTangents { get { return _GetArrayHelper<float>(Enums.InputSemantic.kInTangent); } }
        public _ColladaArray<float> OutTangents { get { return _GetArrayHelper<float>(Enums.InputSemantic.kOutTangent); } }
        public _ColladaArray<string> Interpolations { get { return _GetArrayHelper<string>(Enums.InputSemantic.kInterpolation); } }

    }
}
