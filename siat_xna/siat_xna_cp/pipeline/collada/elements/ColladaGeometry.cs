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
    public sealed class ColladaGeometry : _ColladaElementWithIdAndName
    {
        public ColladaGeometry(XmlReader aReader)
            : base(aReader)
        {
            #region Children
            _NextElement(aReader);
            _AddOptionalChild(aReader, Elements.kAsset);

            // add one and only one of (convex_mesh, mesh, or spline)
            {
                int geometryCount = 0;
                geometryCount += _AddOptionalChild(aReader, Elements.Physics.kConvexMesh);
                geometryCount += _AddOptionalChild(aReader, Elements.kMesh);
                geometryCount += _AddOptionalChild(aReader, Elements.kSpline);

                if (geometryCount != 1)
                {
                    throw new Exception("# of (convex_mesh, mesh, or spline) not equal to 1");
                }
            }

            _AddZeroToManyChildren(aReader, Elements.kExtra);
            #endregion
        }

        public bool IsConvexMesh
        {
            get
            {
                return (GetFirstOptional<physics.ColladaConvexMesh>() != null);
            }
        }

        public physics.ColladaConvexMesh ConvexMesh
        {
            get
            {
                return (GetFirst<physics.ColladaConvexMesh>());
            }
        }

        public bool IsMesh
        {
            get
            {
                return (GetFirstOptional<ColladaMesh>() != null);
            }
        }

        public ColladaMesh Mesh
        {
            get
            {
                return (GetFirst<ColladaMesh>());
            }
        }

        public bool IsSpline
        {
            get
            {
                return (GetFirstOptional<ColladaSpline>() != null);
            }
        }

        public ColladaSpline Spline
        {
            get
            {
                return GetFirst<ColladaSpline>();
            }
        }
    }
}