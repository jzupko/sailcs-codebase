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
    public sealed class ColladaTechniqueHint : _ColladaElement
    {
        #region Private members
        private readonly string mPlatform = "";
        private readonly string mReference;
        private readonly Enums.Profile mProfile;
        #endregion

        public ColladaTechniqueHint(XmlReader aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kPlatform, ref mPlatform);
            _SetRequiredAttribute(aReader, Attributes.kReference, out mReference);

            #region profile
            {
                string profile = "";

                if (_SetOptionalAttribute(aReader, Attributes.kProfile, ref profile))
                {
                    switch (profile)
                    {
                        case Enums.TechniqueHintProfile.kCg: mProfile = Enums.Profile.Cg; break;
                        case Enums.TechniqueHintProfile.kCommon: mProfile = Enums.Profile.Common; break;
                        case Enums.TechniqueHintProfile.kGles: mProfile = Enums.Profile.Gles; break;
                        case Enums.TechniqueHintProfile.kGlsl: mProfile = Enums.Profile.Glsl; break;
                        default:
                            throw new Exception("invalid profile attribute value \"" + profile + "\" of <technique_hint>.");
                    }
                }
            }
            #endregion

            _NextElement(aReader);
            #endregion
        }

        public string Platform { get { return mPlatform; } }
        public string Reference { get { return mReference; } }
        public Enums.Profile Profile { get { return mProfile; } }
    }
}
