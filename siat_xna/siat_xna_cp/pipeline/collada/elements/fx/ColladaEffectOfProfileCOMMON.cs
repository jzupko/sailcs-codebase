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

namespace siat.pipeline.collada.elements.fx
{
    public sealed class ColladaEffectOfProfileCOMMON : _ColladaElement
    {
        #region Protected members
        private Dictionary<string, float> mFloatsCache = new Dictionary<string, float>();
        private Dictionary<string, _ColladaElement> mColorsOrTexturesCache = new Dictionary<string, _ColladaElement>();

        private _ColladaElement mEmission = null;
        private _ColladaElement mAmbient = null;
        private _ColladaElement mDiffuse = null;
        private _ColladaElement mSpecular = null;
        private float mShininess = 0.0f;
        private _ColladaElement mReflective = null;
        private float mReflectivity = 0.0f;
        private TransparencyTypes mTransparencyType = TransparencyTypes.AlphaOne;
        private _ColladaElement mTransparent = null;
        private float mTransparency = 0.0f;
        private float mIndexOfRefraction = 0.0f;
        internal _ColladaElement mBump = null;

        private Enums.EffectType mType;

        private void _HandleInlineColor(XmlReader aReader, ref _ColladaElement arOut)
        {
            string sid = string.Empty;
            _SetOptionalAttribute(aReader, Attributes.kSid, ref sid);

            _ColladaElement element = Elements.kColor.New(aReader);

            if (sid != string.Empty)
            {
                mColorsOrTexturesCache[sid] = element;
            }

            arOut = element;
        }

        private void _HandleReferencedColor(XmlReader aReader, ref _ColladaElement arOut)
        {
            string sid = string.Empty;
            if (_SetOptionalAttribute(aReader, Attributes.kSid, ref sid))
            {
                _ColladaElement value;
                if (mColorsOrTexturesCache.TryGetValue(sid, out value))
                {
                    arOut = value;
                }
            }

            _NextElement(aReader);
        }

        private void _HandleTexture(XmlReader aReader, ref _ColladaElement arOut, ColladaDocument.ResolutionAction aAction)
        {
            string texture = string.Empty;

            if (_SetOptionalAttribute(aReader, Attributes.kTexture, ref texture))
            {
                _ColladaTexture textureEntry = (_ColladaTexture)Elements.FX.kTexture.New(aReader);
                _SetOptionalAttribute(aReader, Attributes.kTexturecoord, ref textureEntry.Texcoords);

                ColladaDocument.QueueSidForResolution(texture, delegate(_ColladaElement e) { textureEntry.Element = e; });
                arOut = textureEntry;
            }

            _NextElement(aReader);
        }

        private void _HandleInlineFloat(XmlReader aReader, ref float arOut)
        {
            string sid = string.Empty;
            _SetOptionalAttribute(aReader, Attributes.kSid, ref sid);

            float value = 0.0f;
            _SetValue(aReader, ref value);

            if (sid != string.Empty)
            {
                mFloatsCache[sid] = value;
            }

            arOut = value;
        }

        private void _HandleReferencedFloat(XmlReader aReader, ref float arOut)
        {
            string sid = string.Empty;
            if (_SetOptionalAttribute(aReader, Attributes.kSid, ref sid))
            {
                float value;
                if (mFloatsCache.TryGetValue(sid, out value))
                {
                    arOut = value;
                }
            }

            _NextElement(aReader);
        }

        private void _HandleFloatParam(XmlReader aReader, string aParamName, ref float arOut)
        {
            if (aParamName == aReader.Name)
            {
                XmlReader subReader = _Sub(aReader);
                _NextElement(subReader);

                if (subReader.Name == Elements.kFloat.Name) _HandleInlineFloat(subReader, ref arOut);
                else if (subReader.Name == Elements.kParam.Name) _HandleReferencedFloat(subReader, ref arOut);
                else
                {
                    throw new Exception("invalid type \"" + subReader.Name + "\"");
                }

                while (subReader.Read()) ;
                _NextElement(aReader);
            }
        }

        private void _HandleColorOrTextureParam(XmlReader aReader, string aParamName, ref _ColladaElement arOut, ColladaDocument.ResolutionAction aAction)
        {
            if (aParamName == aReader.Name)
            {
                XmlReader subReader = _Sub(aReader);

                _NextElement(subReader);

                if (subReader.Name == Elements.kColor.Name) _HandleInlineColor(subReader, ref arOut);
                else if (subReader.Name == Elements.kParam.Name) _HandleReferencedColor(subReader, ref arOut);
                else if (subReader.Name == Elements.FX.kTexture.Name) _HandleTexture(subReader, ref arOut, aAction);
                else
                {
                    throw new Exception("invalid type \"" + subReader.Name + "\"");
                }

                while (subReader.Read()) ;
                _NextElement(aReader);
            }
        }
        #endregion

        public ColladaEffectOfProfileCOMMON(XmlReader aReader, Enums.EffectType aType)
        {
            mType = aType;

            #region Children
            _NextElement(aReader);

            _HandleColorOrTextureParam(aReader, Elements.FX.kEmission.Name, ref mEmission, delegate(_ColladaElement a) { mEmission = a; });
            _HandleColorOrTextureParam(aReader, Elements.FX.kAmbient.Name, ref mAmbient, delegate(_ColladaElement a) { mEmission = a; });
            _HandleColorOrTextureParam(aReader, Elements.FX.kDiffuse.Name, ref mDiffuse, delegate(_ColladaElement a) { mEmission = a; });
            _HandleColorOrTextureParam(aReader, Elements.FX.kSpecular.Name, ref mSpecular, delegate(_ColladaElement a) { mEmission = a; });
            _HandleFloatParam(aReader, Elements.FX.kShininess.Name, ref mShininess);
            _HandleColorOrTextureParam(aReader, Elements.FX.kReflective.Name, ref mReflective, delegate(_ColladaElement a) { mReflective = a; });
            _HandleFloatParam(aReader, Elements.FX.kReflectivity.Name, ref mReflectivity);
            #region Transparency type
            {
                string transparencyType = string.Empty;
                _SetOptionalAttribute(aReader, Attributes.kOpaque, ref transparencyType);
                if (transparencyType == Enums.TransparencyTypes.kAone)
                {
                    mTransparencyType = TransparencyTypes.AlphaOne;
                }
                else if (transparencyType == Enums.TransparencyTypes.kRgbZero)
                {
                    mTransparencyType = TransparencyTypes.RgbZero;
                }
            }
            #endregion
            _HandleColorOrTextureParam(aReader, Elements.FX.kTransparent.Name, ref mTransparent, delegate(_ColladaElement a) { mTransparent = a; });
            _HandleFloatParam(aReader, Elements.FX.kTransparency.Name, ref mTransparency);
            _HandleFloatParam(aReader, Elements.FX.kIndexOfRefraction.Name, ref mIndexOfRefraction);
            #endregion        
        }

        public _ColladaElement Ambient { get { return mAmbient; } }
        public _ColladaElement Bump { get { return mBump; } }
        public _ColladaElement Diffuse { get { return mDiffuse; } }
        public _ColladaElement Emission { get { return mEmission; } }
        public float IndexOfRefraction { get { return mIndexOfRefraction; } }
        public _ColladaElement Reflective { get { return mReflective; } }
        public float Reflectivity { get { return mReflectivity; } }
        public _ColladaElement Specular { get { return mSpecular; } }
        public float Shininess { get { return mShininess; } }
        public _ColladaElement Transparent { get { return mTransparent; } }
        public float Transparency { get { return mTransparency; } }
        public TransparencyTypes TransparencyType { get { return mTransparencyType; } }
        public Enums.EffectType Type { get { return mType; } }
    }

    // Todo: Currently assumes a 2D sampler.
    public sealed class _ColladaTexture : _ColladaElement
    {
        public _ColladaElement Element = null;
        public string Texcoords = string.Empty;

        public ColladaImage Image
        {
            get
            {
                ColladaNewparamOfProfileCOMMON newParam = (ColladaNewparamOfProfileCOMMON)Sampler.Source;
                ColladaSurfaceOfProfileCOMMON surface = newParam.GetFirst<ColladaSurfaceOfProfileCOMMON>();

                return (ColladaImage)surface.Image;
            }
        }

        public ColladaSamplerFX Sampler
        {
            get
            {
                return Element.GetFirst<ColladaSamplerFX>();
            }
        }
    }

    public enum TransparencyTypes
    {
        AlphaOne,
        RgbZero
    }
}
