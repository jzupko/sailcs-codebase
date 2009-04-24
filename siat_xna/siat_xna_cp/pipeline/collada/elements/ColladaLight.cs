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
    public sealed class ColladaLight : _ColladaElementWithIdAndName
    {
        #region Private members
        private readonly float mMayaIntensity = 1.0f;
        private readonly float mMayaDropoff = 0.0f;
        #endregion

        public const string kFColladaExtensions = "FCOLLADA";
        public const string kMayaLightIntensityAttribute = "intensity";
        public const string kMayaLightDropoffAttribute = "dropoff";

        public ColladaLight(XmlReader aReader)
            : base(aReader)
        {
            #region Children
            _NextElement(aReader);
            _AddOptionalChild(aReader, Elements.kAsset);
            _AddRequiredChild(aReader, Elements.kTechniqueCommonOfLight);
            _AddZeroToManyChildren(aReader, Elements.kTechnique);
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion

            #region Maya extra attributes
            ColladaExtra extra = GetFirstOptional<ColladaExtra>();
            if (extra != null)
            {
                ColladaTechnique technique = extra.GetFirst<ColladaTechnique>();
                if (technique.Profile == kFColladaExtensions)
                {
                    foreach (_ColladaElement e in technique.GetEnumerable<_ColladaElement>())
                    {
                        _ColladaGenericElement element = (_ColladaGenericElement)e;

                        if (element.Name == kMayaLightIntensityAttribute)
                        {
                            mMayaIntensity = float.Parse(element.Value);
                        }
                        else if (element.Name == kMayaLightDropoffAttribute)
                        {
                            mMayaDropoff = float.Parse(element.Value);
                        }
                    }
                }
            }
            #endregion
        }

        public float MayaDropoff { get { return mMayaDropoff; } }
        public float MayaIntensity { get { return mMayaIntensity; } }

        public ColladaLightData LightData
        {
            get
            {
                return GetFirst<ColladaTechniqueCommonOfLight>().GetFirst<ColladaLightData>();
            }
        }
    }
}
