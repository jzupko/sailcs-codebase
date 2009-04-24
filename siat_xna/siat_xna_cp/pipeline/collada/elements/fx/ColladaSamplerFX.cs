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
    public sealed class ColladaSamplerFX : _ColladaElement
    {
        #region Private members
        private Enums.SamplerType mType;
        private _ColladaElement mSource;
        private readonly Enums.SamplerWrap mWrapS = Defaults.kWrapS;
        private readonly Enums.SamplerWrap mWrapT = Defaults.kWrapT;
        private readonly Enums.SamplerWrap mWrapP = Defaults.kWrapP;
        private readonly Enums.SamplerFilter mMinfilter = Defaults.kMinfilter;
        private readonly Enums.SamplerFilter mMagfilter = Defaults.kMagfilter;
        private readonly Enums.SamplerFilter mMipfilter = Defaults.kMipfilter;
        private readonly float mBorderColor = Defaults.kBorderColor;
        private readonly uint mMipmapMaxlevel = Defaults.kMipmapMaxlevel;
        private readonly float mMipmapBias = Defaults.kMipmapBias;
        #endregion

        public ColladaSamplerFX(XmlReader aReader, Enums.SamplerType aType)
        {
            mType = aType;

            #region Element values
            _NextElement(aReader);

            string source;
            _SetValueRequired(aReader, Elements.kSource.Name, out source);
            ColladaDocument.QueueSidForResolution(source, delegate(_ColladaElement a) { mSource = a; });

            _SetValueOptional<Enums.SamplerWrap>(aReader, Elements.FX.kWrapS.Name, ref mWrapS, _ColladaElement.Enums.SamplerWrapFromString);
            _SetValueOptional<Enums.SamplerWrap>(aReader, Elements.FX.kWrapT.Name, ref mWrapT, _ColladaElement.Enums.SamplerWrapFromString);
            _SetValueOptional<Enums.SamplerWrap>(aReader, Elements.FX.kWrapP.Name, ref mWrapP, _ColladaElement.Enums.SamplerWrapFromString);
            _SetValueOptional<Enums.SamplerFilter>(aReader, Elements.FX.kMinfilter.Name, ref mMinfilter, _ColladaElement.Enums.SamplerFilterFromString);
            _SetValueOptional<Enums.SamplerFilter>(aReader, Elements.FX.kMagfilter.Name, ref mMagfilter, _ColladaElement.Enums.SamplerFilterFromString);
            _SetValueOptional<Enums.SamplerFilter>(aReader, Elements.FX.kMipfilter.Name, ref mMipfilter, _ColladaElement.Enums.SamplerFilterFromString);
            _SetValueOptional(aReader, Elements.FX.kBorderColor.Name, ref mBorderColor);
            _SetValueOptional(aReader, Elements.FX.kMipmapMaxlevel.Name, ref mMipmapMaxlevel);
            _SetValueOptional(aReader, Elements.FX.kMipmapBias.Name, ref mMipmapBias);
            #endregion

            #region Children
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public _ColladaElement Source { get { return mSource; } }
        public Enums.SamplerWrap WrapS { get { return mWrapS; } }
        public Enums.SamplerWrap WrapT { get { return mWrapT; } }
        public Enums.SamplerWrap WrapP { get { return mWrapP; } }
        public Enums.SamplerFilter Minfilter { get { return mMinfilter; } }
        public Enums.SamplerFilter Magfilter { get { return mMagfilter; } }
        public Enums.SamplerFilter Mipfilter { get { return mMipfilter; } }
        public float BorderColor { get { return mBorderColor; } }
        public uint MipmapMaxlevel { get { return mMipmapMaxlevel; } }
        public float MipmapBias { get { return mMipmapBias; } }
        public Enums.SamplerType Type { get { return mType; } }
    }
}
