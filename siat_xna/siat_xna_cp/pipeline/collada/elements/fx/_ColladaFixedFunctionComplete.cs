using System;
using System.Xml;

namespace siat.pipeline.collada.elements.fx
{
    public abstract class _ColladaFixedFunctionComplete : _ColladaFixedFunction
    {
        #region Protected members
        protected _ColladaElement mSpecular = null;
        protected float mShininess = 0.0f;
        #endregion

        public const string kSpecularElement = "specular";
        public const string kShininessElement = "shininess";

        public _ColladaFixedFunctionComplete(XmlReader aReader)
            : base(aReader)
        { }

        public _ColladaElement Specular
        {
            get
            {
                return mSpecular;
            }
        }

        public float Shininess
        {
            get
            {
                return mShininess;
            }
        }
    }
}
