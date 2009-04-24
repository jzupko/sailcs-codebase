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
using System;
using System.Collections.Generic;
using System.IO;

namespace siat
{
    public class TriangleTree : kdTree<Triangle>
    {
        public static readonly kdTreeCoefficients kCoefficients
            = new kdTreeCoefficients(0.25f, 0.25f, 1.0f);

        public TriangleTree() : this(kCoefficients, kMaximumDepth) { }
        public TriangleTree(kdTreeCoefficients aCoeff) : this(aCoeff, kMaximumDepth) { }
        public TriangleTree(kdTreeCoefficients aCoeff, int aDepth) : base(aCoeff, aDepth) { }

        public void Build(IList<Triangle> aTriangles)
        {
            _Build(aTriangles);
        }

        public void Read(BinaryReader aReader)
        {
            mCoeff.Intersection = aReader.ReadSingle();
            mCoeff.Localization = aReader.ReadSingle();
            mCoeff.Split = aReader.ReadSingle();
            mDepth = aReader.ReadInt32();
            mNodeCount = aReader.ReadInt32();
            mNodes = new Node[mNodeCount];

            for (int i = 0; i < mNodeCount; i++)
            {
                mNodes[i].AABB.Max.X = aReader.ReadSingle();
                mNodes[i].AABB.Max.Y = aReader.ReadSingle();
                mNodes[i].AABB.Max.Z = aReader.ReadSingle();
                mNodes[i].AABB.Min.X = aReader.ReadSingle();
                mNodes[i].AABB.Min.Y = aReader.ReadSingle();
                mNodes[i].AABB.Min.Z = aReader.ReadSingle();
                mNodes[i].Sibling = aReader.ReadInt32();
                mNodes[i].TotalFacesInSubtree = aReader.ReadInt32();
                int triangleCount = aReader.ReadInt32();
                mNodes[i].Objects = new List<Triangle>();
                for (int j = 0; j < triangleCount; j++)
                {
                    Vector3 p0;
                    Vector3 p1;
                    Vector3 p2;
                    p0.X = aReader.ReadSingle();
                    p0.Y = aReader.ReadSingle();
                    p0.Z = aReader.ReadSingle();
                    p1.X = aReader.ReadSingle();
                    p1.Y = aReader.ReadSingle();
                    p1.Z = aReader.ReadSingle();
                    p2.X = aReader.ReadSingle();
                    p2.Y = aReader.ReadSingle();
                    p2.Z = aReader.ReadSingle();

                    mNodes[i].Objects.Add(new Triangle(p0, p1, p2));
                }
            }
        }

        public void Write(BinaryWriter aWriter)
        {
            aWriter.Write(mCoeff.Intersection);
            aWriter.Write(mCoeff.Localization);
            aWriter.Write(mCoeff.Split);
            aWriter.Write(mDepth);
            aWriter.Write(mNodeCount);

            for (int i = 0; i < mNodeCount; i++)
            {
                aWriter.Write(mNodes[i].AABB.Max.X);
                aWriter.Write(mNodes[i].AABB.Max.Y);
                aWriter.Write(mNodes[i].AABB.Max.Z);
                aWriter.Write(mNodes[i].AABB.Min.X);
                aWriter.Write(mNodes[i].AABB.Min.Y);
                aWriter.Write(mNodes[i].AABB.Min.Z);
                aWriter.Write(mNodes[i].Sibling);
                aWriter.Write(mNodes[i].TotalFacesInSubtree);
                aWriter.Write(mNodes[i].Objects.Count);
                int triangleCount = mNodes[i].Objects.Count;
                for (int j = 0; j < triangleCount; j++)
                {
                    aWriter.Write(mNodes[i].Objects[j].P0.X);
                    aWriter.Write(mNodes[i].Objects[j].P0.Y);
                    aWriter.Write(mNodes[i].Objects[j].P0.Z);
                    aWriter.Write(mNodes[i].Objects[j].P1.X);
                    aWriter.Write(mNodes[i].Objects[j].P1.Y);
                    aWriter.Write(mNodes[i].Objects[j].P1.Z);
                    aWriter.Write(mNodes[i].Objects[j].P2.X);
                    aWriter.Write(mNodes[i].Objects[j].P2.Y);
                    aWriter.Write(mNodes[i].Objects[j].P2.Z);
                }
            }
        }
    }

}
