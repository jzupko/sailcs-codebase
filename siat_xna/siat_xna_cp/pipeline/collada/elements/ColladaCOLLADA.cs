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
    public sealed class ColladaCOLLADA : _ColladaElement
    {
        #region Private members
        private readonly string mSchema;
        private readonly string mSourceFile = "";
        #endregion

        public const string kLibrary = "library_";

        public ColladaCOLLADA(XmlReader aReader, string aSourceFile)
        {
            mSourceFile = aSourceFile;

            #region Attributes

            #region Version
            string colladaVersion;
            _SetRequiredAttribute(aReader, Attributes.kVersion, out colladaVersion);
            if (Array.IndexOf<string>(kColladaSupportedVersions, colladaVersion) < 0)
            {
                throw new Exception("Unsupported COLLADA version.");
            }
            #endregion

            _SetRequiredAttribute(aReader, Attributes.kSchema, out mSchema);
            #endregion

            #region Children
            _NextElement(aReader);
            _AddRequiredChild(aReader, Elements.kAsset);

            while (aReader.Name.StartsWith(kLibrary))
            {
                _AddOptionalChild(aReader, Elements.kLibraryAnimations);
                _AddOptionalChild(aReader, Elements.kLibraryAnimationClips);
                _AddOptionalChild(aReader, Elements.Physics.kLibraryPhysicsMaterials);
                _AddOptionalChild(aReader, Elements.Physics.kLibraryPhysicsModels);
                _AddOptionalChild(aReader, Elements.Physics.kLibraryPhysicsScenes);
                _AddOptionalChild(aReader, Elements.Physics.kLibraryForceFields);
                _AddOptionalChild(aReader, Elements.kLibraryCameras);
                _AddOptionalChild(aReader, Elements.kLibraryLights);
                _AddOptionalChild(aReader, Elements.kLibraryImages);
                _AddOptionalChild(aReader, Elements.FX.kLibraryMaterials);
                _AddOptionalChild(aReader, Elements.FX.kLibraryEffects);
                _AddOptionalChild(aReader, Elements.kLibraryGeometries);
                _AddOptionalChild(aReader, Elements.kLibraryControllers);
                _AddOptionalChild(aReader, Elements.kLibraryVisualScenes);
                _AddOptionalChild(aReader, Elements.kLibraryNodes);
            }
            _AddOptionalChild(aReader, Elements.kScene);
            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public string SourceFile { get { return mSourceFile; } }
    }
}
