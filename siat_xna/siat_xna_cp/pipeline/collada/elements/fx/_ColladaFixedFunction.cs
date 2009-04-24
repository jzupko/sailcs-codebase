using System;
using System.Collections.Generic;
using System.Xml;

namespace siat.pipeline.collada.elements.fx
{
    public abstract class _ColladaFixedFunction : _ColladaFixedFunctionBase
    {
        #region Protected members
        protected _ColladaElement mAmbient = null;
        protected _ColladaElement mDiffuse = null;
        #endregion

        public const string kAmbientElement = "ambient";
        public const string kDiffuseElement = "diffuse";

        public _ColladaFixedFunction(XmlReader aReader)
            : base(aReader)
        { }

        public _ColladaElement Ambient
        {
            get
            {
                return mAmbient;
            }
        }

        public _ColladaElement Diffuse
        {
            get
            {
                return mDiffuse;
            }
        }
    }
}
