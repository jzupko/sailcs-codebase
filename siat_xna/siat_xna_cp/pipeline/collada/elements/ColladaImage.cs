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
using System.IO;
using System.Xml;

namespace siat.pipeline.collada.elements
{
    // Todo: need to support <data> element for embedded image data,
    //    first need to figure out what the format is precisely.
    public sealed class ColladaImage : _ColladaElementWithIdAndName
    {
        #region Private members
        private readonly string mFormat = "";
        private readonly uint mHeight = 0;
        private readonly uint mWidth = 0;
        private readonly uint mDepth = Defaults.kImageDepth;
        private readonly object mDataOrLocation;
        #endregion

        public ColladaImage(XmlReader aReader)
            : base(aReader)
        {
            #region Attributes
            _SetOptionalAttribute(aReader, Attributes.kFormat, ref mFormat);
            _SetOptionalAttribute(aReader, Attributes.kHeight, ref mHeight);
            _SetOptionalAttribute(aReader, Attributes.kWidth, ref mWidth);
            _SetOptionalAttribute(aReader, Attributes.kDepth, ref mDepth);
            #endregion

            #region Children
            _NextElement(aReader);
            _AddOptionalChild(aReader, Elements.kAsset);

            #region <init_from> or <data>
            {
                byte[] data = null;
                string location = string.Empty;

                _SetValueOptional(aReader, Elements.kInitFrom.Name, ref location);
                _SetValueOptional(aReader, Elements.kData.Name, ref data);

                if (data != null && location != string.Empty)
                {
                    throw new Exception("one and only one of <data> or <init_from> must be defined under element <image>.");
                }
                else if (data != null)
                {
                    mDataOrLocation = data;
                }
                else
                {
                    mDataOrLocation = PipelineUtilities.FromUriFileToPath(ColladaDocument.CurrentBase, location);
                }
            }
            #endregion

            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public byte[] Data { get { return (byte[])mDataOrLocation; } }
        public uint Depth { get { return mDepth; } }
        public string Format { get { return mFormat; } }
        public uint Height { get { return mHeight; } }
        public bool IsData { get { return (mDataOrLocation is byte[]); } }
        public bool IsLocation { get { return (mDataOrLocation is string); } }
        public string Location { get { return (string)mDataOrLocation; } }
        public uint Width { get { return mWidth; } }
    }
}
