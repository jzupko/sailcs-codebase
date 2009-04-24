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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using siat.pipeline.collada;
using siat.pipeline.collada.elements;

namespace siat.pipeline
{
    public static class PipelineUtilities
    {
        #region Helpers to apply a transform to a mesh.
        private static bool _GetShouldTransform(VertexElementUsage aUsage)
        {
            switch (aUsage)
            {
                case VertexElementUsage.Binormal: return true;
                case VertexElementUsage.Normal: return true;
                case VertexElementUsage.Position: return true;
                case VertexElementUsage.Tangent: return true;
                default:
                    return false;
            }
        }

        private static float _GetWcomponent(VertexElementUsage aUsage)
        {
            return ((aUsage == VertexElementUsage.Position) ? 1.0f : 0.0f);
        }
        #endregion

        public static void ApplyTransform(SiatMeshContent aMesh, ref Matrix aTransform)
        {
            foreach (SiatMeshContent.Part e in aMesh.Parts)
            {
                ApplyTransform(e, ref aTransform);
            }
        }

        public static void ApplyTransform(SiatMeshContent.Part aMeshPart, ref Matrix aTransform)
        {
            int stride = aMeshPart.VertexStrideInSingles;
            int count = aMeshPart.VertexCount * stride;
            foreach (VertexElement e in aMeshPart.VertexDeclaration)
            {
                if (_GetShouldTransform(e.VertexElementUsage))
                {
                    int start = e.Offset / sizeof(float);
                    if (e.VertexElementFormat == VertexElementFormat.Vector3)
                    {
                        float w = _GetWcomponent(e.VertexElementUsage);
                        for (int i = start; i < count; i += stride)
                        {
                            Vector4 v = new Vector4(aMeshPart.Vertices[i + 0], aMeshPart.Vertices[i + 1], aMeshPart.Vertices[i + 2], w);
                            Vector4.Transform(ref v, ref aTransform, out v);
                            aMeshPart.Vertices[i + 0] = v.X;
                            aMeshPart.Vertices[i + 1] = v.Y;
                            aMeshPart.Vertices[i + 2] = v.Z;
                        }
                    }
                    else if (e.VertexElementFormat == VertexElementFormat.Vector4)
                    {
                        for (int i = start; i < count; i += stride)
                        {
                            Vector4 v = new Vector4(aMeshPart.Vertices[i + 0], aMeshPart.Vertices[i + 1], aMeshPart.Vertices[i + 2], aMeshPart.Vertices[i + 3]);
                            Vector4.Transform(ref v, ref aTransform, out v);
                            aMeshPart.Vertices[i + 0] = v.X;
                            aMeshPart.Vertices[i + 1] = v.Y;
                            aMeshPart.Vertices[i + 2] = v.Z;
                            aMeshPart.Vertices[i + 3] = v.W;
                        }
                    }
                    else
                    {
                        throw new Exception(Utilities.kShouldNotBeHere);
                    }
                }
            }
        }

        public static PrimitiveType ColladaPrimitiveToXnaPrimitive(_ColladaPrimitive aColladaPrimitive)
        {
            if (aColladaPrimitive is ColladaTriangles) { return PrimitiveType.TriangleList; }
            else if (aColladaPrimitive is ColladaTrifans) { return PrimitiveType.TriangleFan; }
            else if (aColladaPrimitive is ColladaTristrips) { return PrimitiveType.TriangleStrip; }
            else if (aColladaPrimitive is ColladaLines) { return PrimitiveType.LineList; }
            else if (aColladaPrimitive is ColladaLinestrips) { return PrimitiveType.LineStrip; }
            else
            {
                throw new Exception("The COLLADA primitive type \"" + aColladaPrimitive.GetType().Name + "\" is not yet supported.");
            }
        }

        public static VertexElementFormat ColladaSemanticToXnaFormat(string aColladaSemantic, uint aStride)
        {
            switch (aStride)
            {
                case 1u: return VertexElementFormat.Single;
                case 2u: return VertexElementFormat.Vector2;
                case 3u: return VertexElementFormat.Vector3;
                case 4u: return VertexElementFormat.Vector4;
                default:
                    throw new Exception("Cannot convert COLLADA semantic \"" + aColladaSemantic + "\" with stride " + aStride.ToString() + " to an XNA vertex format.");
            }
        }

        public static VertexElementUsage ColladaSemanticToXnaUsage(string aColladaSemantic)
        {
            switch (aColladaSemantic)
            {
                case _ColladaElement.Enums.InputSemantic.kColor: return VertexElementUsage.Color;
                case _ColladaElement.Enums.InputSemantic.kNormal: return VertexElementUsage.Normal;
                case _ColladaElement.Enums.InputSemantic.kPosition: return VertexElementUsage.Position;
                case _ColladaElement.Enums.InputSemantic.kTexbinormal: return VertexElementUsage.Binormal;
                case _ColladaElement.Enums.InputSemantic.kTexcoord: return VertexElementUsage.TextureCoordinate;
                case _ColladaElement.Enums.InputSemantic.kTextangent: return VertexElementUsage.Tangent;
                default:
                    throw new Exception("cannot convert COLLADA semantic \"" + aColladaSemantic + "\" to an XNA vertex usage.");
            }
        }

        public static string ColladaSurfaceFilterToHlsl(_ColladaElement.Enums.SamplerFilter aFilter)
        {
            // const string kAnisotropic = "Anisotropic";
            const string kLinear = "Linear";
            const string kNone = "None";
            const string kPoint = "Point";

            switch (aFilter)
            {
                case _ColladaElement.Enums.SamplerFilter.Linear: return kLinear;
                case _ColladaElement.Enums.SamplerFilter.LinearMipmapLinear: return kLinear;
                case _ColladaElement.Enums.SamplerFilter.LinearMipmapNearest: return kLinear;
                case _ColladaElement.Enums.SamplerFilter.Nearest: return kPoint;
                case _ColladaElement.Enums.SamplerFilter.NearestMipmapLinear: return kPoint;
                case _ColladaElement.Enums.SamplerFilter.NearestMipmapNearest: return kPoint;
                case _ColladaElement.Enums.SamplerFilter.None: return kNone;
                default:
                    throw new Exception(Utilities.kShouldNotBeHere);
            }
        }

        public static string ColladaSurfaceWrapToHlsl(_ColladaElement.Enums.SamplerWrap aWrap)
        {
            const string kBorder = "Border";
            const string kClamp = "Clamp";
            const string kMirror = "Mirror";
            const string kWrap = "Wrap";

            switch (aWrap)
            {
                case _ColladaElement.Enums.SamplerWrap.Border: return kBorder;
                case _ColladaElement.Enums.SamplerWrap.Clamp: return kClamp;
                case _ColladaElement.Enums.SamplerWrap.Mirror: return kMirror;
                case _ColladaElement.Enums.SamplerWrap.None: return kBorder;
                case _ColladaElement.Enums.SamplerWrap.Wrap: return kWrap;
                default:
                    throw new Exception(Utilities.kShouldNotBeHere);
            }
        }

        public static void ExtractCompactPositions(SiatMeshContent.Part aPart, out List<Vector3> arPositions)
        {
            arPositions = new List<Vector3>();

            int offset = FindOffsetInSingles(aPart, VertexElementUsage.Position, 0);
            int stride = aPart.VertexStrideInSingles;
            int count = aPart.VertexCount * stride;

            for (int i = offset; i < count; i += stride)
            {
                Vector3 v = new Vector3(aPart.Vertices[i + 0], aPart.Vertices[i + 1], aPart.Vertices[i + 2]);
                if (!arPositions.Exists(delegate(Vector3 e) { return Utilities.AboutEqual(v, e, Utilities.kLooseToleranceFloat); }))
                {
                    arPositions.Add(v);
                }
            }
        }

        public static float[] ExtractPositions(SiatMeshContent.Part aPart)
        {
            List<float> ret = new List<float>();

            int offset = FindOffsetInSingles(aPart, VertexElementUsage.Position, 0);
            int stride = aPart.VertexStrideInSingles;
            int count = aPart.VertexCount * stride;

            for (int i = offset; i < count; i += stride)
            {
                ret.Add(aPart.Vertices[i + 0]);
                ret.Add(aPart.Vertices[i + 1]);
                ret.Add(aPart.Vertices[i + 2]);
            }

            return ret.ToArray();
        }

        public static void ExtractPositions(SiatMeshContent.Part aPart, out List<Vector3> arPositions)
        {
            float[] p = ExtractPositions(aPart);
            int count = p.Length;

            arPositions = new List<Vector3>();
            for (int i = 0; i < count; i += 3)
            {
                arPositions.Add(new Vector3(p[i + 0], p[i + 1], p[i + 2]));
            }
        }

        public static void ExtractSkinning(SiatMeshContent.Part aPart, out List<Vector4> arBoneIndices, out List<Vector4> arBoneWeights)
        {
            arBoneIndices = new List<Vector4>();
            arBoneWeights = new List<Vector4>();

            #region Indices
            {
                int offset = FindOffsetInSingles(aPart, VertexElementUsage.BlendIndices, 0);
                int stride = aPart.VertexStrideInSingles;
                int count = aPart.VertexCount * stride;

                for (int i = offset; i < count; i += stride)
                {
                    float x = aPart.Vertices[i + 0];
                    float y = aPart.Vertices[i + 1];
                    float z = aPart.Vertices[i + 2];
                    float w = aPart.Vertices[i + 3];

                    arBoneIndices.Add(new Vector4(x, y, z, w));
                }
            }
            #endregion

            #region Weights
            {
                int offset = FindOffsetInSingles(aPart, VertexElementUsage.BlendWeight, 0);
                int stride = aPart.VertexStrideInSingles;
                int count = aPart.VertexCount * stride;

                for (int i = offset; i < count; i += stride)
                {
                    float x = aPart.Vertices[i + 0];
                    float y = aPart.Vertices[i + 1];
                    float z = aPart.Vertices[i + 2];
                    float w = aPart.Vertices[i + 3];

                    arBoneWeights.Add(new Vector4(x, y, z, w));
                }
            }
            #endregion
        }

        public static void ExtractTriangles(SiatMeshContent.Part aPart, out List<Vector3> arPositions)
        {
            List<Vector3> buf = new List<Vector3>();

            int offset = FindOffsetInSingles(aPart, VertexElementUsage.Position, 0);
            int stride = aPart.VertexStrideInSingles;
            int count = aPart.VertexCount * stride;

            for (int i = offset; i < count; i += stride)
            {
                Vector3 v = new Vector3(aPart.Vertices[i + 0], aPart.Vertices[i + 1], aPart.Vertices[i + 2]);
                buf.Add(v);
            }

            arPositions = new List<Vector3>();

            if (aPart.Indices != null)
            {
                foreach (int e in aPart.Indices)
                {
                    arPositions.Add(buf[e]);
                }
            }
        }

        public static void ExtractTriangles(SiatMeshContent aMesh, out List<Vector3> arPositions)
        {
            arPositions = new List<Vector3>();
            List<Vector3> buf = new List<Vector3>();

            foreach (SiatMeshContent.Part e in aMesh.Parts)
            {
                ExtractTriangles(e, out buf);
                arPositions.AddRange(buf);
            }
        }

        /// <summary>
        /// Given a filename returns the relative path and filename of the file after XNA content processing.
        /// </summary>
        /// <param name="aFilename">The filename of a content input file.</param>
        /// <returns>The expected filename of the content after processing, without the .xnb extension.</returns>
        /// <remarks>
        /// There oddly isn't an easy way to get the filename and path of the resulting XNA content file
        /// after processing in the pipeline from the input file. This function accomplishes this by
        /// assuming that all files are contained within Siat.kMediaRoot after processing.
        /// </remarks>
        public static string ExtractXnaAssetName(string aFilename)
        {
            string ret = aFilename;
            int index = aFilename.LastIndexOf(Utilities.kMediaRoot);
            if (index >= 0)
            {
                ret = aFilename.Substring(index + Utilities.kMediaRoot.Length);
            }

            ret = Utilities.RemoveExtension(ret);
            ret = ret.Trim();

            return ret;
        }

        /// <summary>
        /// Finds the starting offset in singles to a specific vertex element.
        /// </summary>
        /// <param name="aPart">The part.</param>
        /// <param name="aUsage">The usage of the element of interest.</param>
        /// <param name="aUsageIndex">The usage index of the element of interest.</param>
        /// <returns>A positive offset on success, a negative offset on failure.</returns>
        /// <remarks>
        /// for (int i = offset; i < (aPart.VertexCount * aPart.VertexStride); i += aPart.VertexStride) {} can be used
        /// to iterate over the specific element of the vertex buffer.
        /// </remarks>
        public static int FindOffsetInSingles(SiatMeshContent.Part aPart, VertexElementUsage aUsage, byte aUsageIndex)
        {
            VertexElement[] decl = aPart.VertexDeclaration;

            int offset = 0;
            if (decl != null)
            {
                int elementCount = decl.Length;
                

                foreach (VertexElement f in decl)
                {
                    if (f.VertexElementUsage == aUsage && f.UsageIndex == aUsageIndex) { break; }
                    else { offset = (f.Offset / sizeof(float)); }
                }
            }
            return offset;
        }

        /// <summary>
        /// Converts a URI to a local file system path.
        /// </summary>
        /// <param name="aUriBase">The base of the uri.</param>
        /// <param name="aUriFile">The input uri.</param>
        /// <returns>A local path to the file.</returns>
        /// <remarks>
        /// This function does not checking beyond what it is inherent in System.Uri. Therefore,
        /// the returned string will be syntactically correct but not necessarily semantically
        /// correct (i.e. the "file" in the path may not even be a real file).
        /// </remarks>
        public static string FromUriFileToPath(string aUriBase, string aUriFile)
        {
            string ret = Uri.UnescapeDataString(aUriFile);
            Uri uri = new Uri(new Uri(aUriBase), ret);
            ret = uri.AbsolutePath;
            ret = Uri.UnescapeDataString(ret);
            ret = Path.GetFullPath(ret);

            return ret;
        }

        /// <summary>
        /// Simple helper function to overcome CompilerMacro's lack of a constructor.
        /// </summary>
        /// <param name="aName">Name of the macro.</param>
        /// <param name="aDefinition">Macro definition.</param>
        /// <returns>A CompilerMacro object with the given name and definition.</returns>
        public static CompilerMacro NewMacro(string aName, string aDefinition)
        {
            CompilerMacro ret = new CompilerMacro();
            ret.Name = aName;
            ret.Definition = aDefinition;

            return ret;
        }

        /// <summary>
        /// Helper class to compare two vertex element arrays by value.
        /// </summary>
        public sealed class VertexDeclarationComparer : IEqualityComparer<VertexElement[]>
        {
            public bool Equals(VertexElement[] a, VertexElement[] b)
            {
                if (a.Length != b.Length)
                {
                    return false;
                }
                else
                {
                    int count = a.Length;
                    for (int i = 0; i < count; i++)
                    {
                        if (a[i] != b[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            public int GetHashCode(VertexElement[] aDeclaration)
            {
                string hashString = string.Empty;
                foreach (VertexElement e in aDeclaration)
                {
                    hashString += e.ToString();
                }
                int hash = (int)Hash.Calculate32(hashString, 0u);

                return hash;
            }
        }
    }
}
