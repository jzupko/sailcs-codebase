using System;
using System.Collections.Generic;
using System.Xml;

namespace siat.pipeline.collada.elements.fx
{
    // Todo: Currently assumes a 2D sampler.
    public sealed class _ColladaTexture : _ColladaElement
    {
        public _ColladaElement Element = null;
        public string Texcoords = "";

        public ColladaImage Image
        {
            get
            {
                ColladaNewparamOfProfileCOMMON newParam = (ColladaNewparamOfProfileCOMMON)Sampler2D.Source;
                ColladaSurfaceOfProfileCOMMON surface = newParam.GetFirst<ColladaSurfaceOfProfileCOMMON>();

                return (ColladaImage)surface.Image;
            }
        }

        public bool HasSampler2D
        {
            get
            {
                return (Element.GetFirstOptional<ColladaSampler2D>() != null);
            }
        }

        public ColladaSampler2D Sampler2D
        {
            get
            {
                return Element.GetFirst<ColladaSampler2D>();
            }
        }
    }

    public enum TransparencyTypes
    {
        AlphaOne,
        RgbZero
    }

    public abstract class _ColladaFixedFunctionBase : _ColladaElement
    {
        #region Protected members
        protected _ColladaElement mEmission = null;
        protected _ColladaElement mReflective = null;
        protected float mReflectivity = 0.0f;
        protected TransparencyTypes mTransparencyType = TransparencyTypes.AlphaOne;
        protected _ColladaElement mTransparent = null;
        protected float mTransparency = 0.0f;
        protected float mIndexOfRefraction = 0.0f;

        protected void _HandleInlineColor(XmlReader aReader, ref Dictionary<string, _ColladaElement> aCache, ref _ColladaElement arOut)
        {
            string sid = "";
            _SetOptionalAttribute(aReader, Attributes.kSid, ref sid);

            _ColladaElement element = new ColladaColor(aReader);

            if (sid != "")
            {
                aCache[sid] = element;
            }

            arOut = element;
        }

        protected void _HandleReferencedColor(XmlReader aReader, Dictionary<string, _ColladaElement> aCache, ref _ColladaElement arOut)
        {
            string sid = "";
            if (_SetOptionalAttribute(aReader, Attributes.kSid, ref sid))
            {
                _ColladaElement value;
                if (aCache.TryGetValue(sid, out value))
                {
                    arOut = value;
                }
            }

            _NextElement(aReader);
        }

        protected void _HandleTexture(XmlReader aReader, ref _ColladaElement arOut, ColladaDocument.ResolutionAction aAction)
        {
            string texture = "";

            if (_SetOptionalAttribute(aReader, Attributes.kTexture, ref texture))
            {
                _ColladaTexture textureEntry = new _ColladaTexture();
                _SetOptionalAttribute(aReader, Attributes.kTexturecoord, ref textureEntry.Texcoords);

                ColladaDocument.QueueSidForResolution(texture, delegate(_ColladaElement e) { textureEntry.Element = e; });
                arOut = textureEntry;
            }

            _NextElement(aReader);
        }

        protected void _HandleInlineFloat(XmlReader aReader, ref Dictionary<string, float> aCache, ref float arOut)
        {
            string sid = "";
            _SetOptionalAttribute(aReader, Attributes.kSid, ref sid);

            float value = 0.0f;
            _SetValue(aReader, ref value);

            if (sid != "")
            {
                aCache[sid] = value;
            }

            arOut = value;
        }

        protected void _HandleReferencedFloat(XmlReader aReader, Dictionary<string, float> aCache, ref float arOut)
        {
            string sid = "";
            if (_SetOptionalAttribute(aReader, Attributes.kSid, ref sid))
            {
                float value;
                if (aCache.TryGetValue(sid, out value))
                {
                    arOut = value;
                }
            }

            _NextElement(aReader);
        }

        protected void _HandleFloatParam(XmlReader aReader, ref Dictionary<string, float> aCache, string aParamName, ref float arOut)
        {
            if (aParamName == aReader.Name)
            {
                XmlReader subReader = _Sub(aReader);
                _NextElement(subReader);

                switch (aReader.Name)
                {
                    case kFloatElement: _HandleInlineFloat(subReader, ref aCache, ref arOut); break;
                    case kParamElement: _HandleReferencedFloat(subReader, aCache, ref arOut); break;
                    default:
                        throw new Exception("invalid type \"" + subReader.Name + "\"");
                }

                while (subReader.Read()) ;
                _NextElement(aReader);
            }
        }

        protected void _HandleColorOrTextureParam(XmlReader aReader, ref Dictionary<string, _ColladaElement> aCache, string aParamName, ref _ColladaElement arOut, ColladaDocument.ResolutionAction aAction)
        {
            if (aParamName == aReader.Name)
            {
                XmlReader subReader = _Sub(aReader);

                _NextElement(subReader);

                switch (subReader.Name)
                {
                    case kColorElement: _HandleInlineColor(subReader, ref aCache, ref arOut); break;
                    case kParamElement: _HandleReferencedColor(subReader, aCache, ref arOut); break;
                    case kTextureElement: _HandleTexture(subReader, ref arOut, aAction); break;
                    default:
                        throw new Exception("invalid type \"" + subReader.Name + "\"");
                }

                while (subReader.Read()) ;
                _NextElement(aReader);
            }
        }
        #endregion

        public const string kEmissionElement = "emission";
        public const string kReflectiveElement = "reflective";
        public const string kReflectivityElement = "reflectivity";
        public const string kTransparentElement = "transparent";
        public const string kTransparencyElement = "transparency";
        public const string kIndexOfRefractionElement = "index_of_refraction";

        public const string kFloatElement = "float";
        public const string kParamElement = "param";
        public const string kColorElement = "color";
        public const string kTextureElement = "texture";

        public _ColladaFixedFunctionBase(XmlReader aReader)
        { }

        public _ColladaElement Emission
        {
            get
            {
                return mEmission;
            }
        }

        public _ColladaElement Reflective
        {
            get
            {
                return mReflective;
            }
        }

        public float Reflectivity
        {
            get
            {
                return mReflectivity;
            }
        }

        public _ColladaElement Transparent
        {
            get
            {
                return mTransparent;
            }
        }

        public float Transparency
        {
            get
            {
                return mTransparency;
            }
        }

        public TransparencyTypes TransparencyType
        {
            get
            {
                return mTransparencyType;
            }
        }

        public float IndexOfRefraction
        {
            get
            {
                return mIndexOfRefraction;
            }
        }
    }
}
