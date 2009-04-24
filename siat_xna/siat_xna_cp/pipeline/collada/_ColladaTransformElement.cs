using Microsoft.Xna.Framework;
using System;
using System.Xml;


namespace siat.pipeline.collada.elements
{
    public abstract class _ColladaTransformElement : _ColladaChannelTarget
    {
        public _ColladaTransformElement(XmlReader aReader)
            : base(aReader)
        { }

        public abstract Matrix XnaMatrix { get; }
    }

}
