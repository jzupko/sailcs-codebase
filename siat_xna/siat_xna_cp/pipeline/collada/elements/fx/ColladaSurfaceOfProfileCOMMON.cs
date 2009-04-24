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
    public sealed class ColladaSurfaceOfProfileCOMMON : _ColladaElement
    {
        #region Private members
        private readonly Enums.SurfaceType mType;
        private ColladaImage mImage = null;
        private readonly string mFormat = Defaults.kFormat;
        private readonly uint mMipLevels = 0;
        private readonly bool mbGenerateMipMaps = false;
        private readonly uint mHeight = 0;
        private readonly uint mWidth = 0;
        private readonly uint mDepth = 0;
        #endregion

        public ColladaSurfaceOfProfileCOMMON(XmlReader aReader)
        {
            #region Attributes
            #region type
            {
                string type;
                _SetRequiredAttribute(aReader, Attributes.kType, out type);
                switch (type)
                {
                    case Enums.SurfaceTypeAttribute.kUntyped: mType = Enums.SurfaceType.Untyped; break;
                    case Enums.SurfaceTypeAttribute.k1d: mType = Enums.SurfaceType.OneD; break;
                    case Enums.SurfaceTypeAttribute.k2d: mType = Enums.SurfaceType.TwoD; break;
                    case Enums.SurfaceTypeAttribute.k3d: mType = Enums.SurfaceType.ThreeD; break;
                    case Enums.SurfaceTypeAttribute.kCube: mType = Enums.SurfaceType.Cube; break;
                    case Enums.SurfaceTypeAttribute.kDepth: mType = Enums.SurfaceType.Depth; break;
                    case Enums.SurfaceTypeAttribute.kRectangle: mType = Enums.SurfaceType.Rectangle; break;
                    default:
                        throw new Exception("invalid surface type for <surface>.");
                }
            }
            #endregion
            #endregion

            #region Children
            _NextElement(aReader);
            // Todo: support inits other than init_from
            #region initialization option
            {
                string imageId = "";
                _SetValueOptional(aReader, Elements.kInitFrom.Name, ref imageId);
                // Note: all ids for resolution need to have a # in front if they are local
                if (!imageId.Contains("#"))
                {
                    imageId = "#" + imageId;
                }

                ColladaDocument.QueueIdForResolution(imageId, delegate(_ColladaElement aResolvedElement) { mImage = (ColladaImage)aResolvedElement; });
            }
            #endregion

            _SetValueOptional(aReader, Elements.FX.kFormat.Name, ref mFormat);
            _AddOptionalChild(aReader, Elements.FX.kFormatHint);

            #region <size> and <viewport_ratio>
            {
                string size = "";
                uint ratio = 0;
                if (_SetValueOptional(aReader, Elements.FX.kSize.Name, ref size))
                {
                    uint[] buf = new uint[3];
                    Utilities.Tokenize(size, buf, XmlConvert.ToUInt32);
                    mWidth = buf[0];
                    mHeight = buf[1];
                    mDepth = buf[2];
                }
                else if (_SetValueOptional(aReader, Elements.FX.kViewportRatio.Name, ref ratio))
                {
                    throw new Exception(Utilities.kNotImplemented);
                }
            }
            #endregion

            _SetValueOptional(aReader, Elements.FX.kMipLevels.Name, ref mMipLevels);
            _SetValueOptional(aReader, Elements.FX.kMipmapGenerate.Name, ref mbGenerateMipMaps);
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public Enums.SurfaceType Type { get { return mType; } }
        public ColladaImage Image { get { return mImage; } }
        public string Format { get { return mFormat; } }
        public uint MipLevels { get { return mMipLevels; } }
        public bool bGenerateMipMaps { get { return mbGenerateMipMaps; } }
        public uint Height { get { return mHeight; } }
        public uint Width { get { return mWidth; } }
        public uint Depth { get { return mDepth; } }
    }
}
