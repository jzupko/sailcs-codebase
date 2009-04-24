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

namespace siat.render
{
    /// <summary>
    /// Contains the index buffer, vertex buffer, vertex declaration, and other data that
    /// is actually used to represent geometry to render.
    /// </summary>
    public sealed class MeshPart
    {
        public MeshPart() : this(string.Empty) { }
        public MeshPart(string aId)
        {
            Id = aId;
        }

        public readonly string Id;
        public IndexBuffer Indices;
        public BoundingBox AABB;
        public BoundingSphere BoundingSphere;
        public int PrimitiveCount;
        public PrimitiveType PrimitiveType;
        public VertexBuffer Vertices;
        public int VertexCount;
        public VertexDeclaration VertexDeclaration;
        public int VertexStride;
    }

    public sealed class UserPrimitives
    {
        public int PrimitiveCount;
        public PrimitiveType PrimitiveType;
        public Vector3[] Vertices;
        public VertexDeclaration VertexDeclaration;
    }
}
