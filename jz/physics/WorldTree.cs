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
using System.Text;
using siat;
using jz.physics.narrowphase;

namespace jz.physics
{
    public class WorldTree : TriangleTree
    {
        public WorldTree() : this(DefaultCoefficients, kMaximumDepth) { }
        public WorldTree(kdTreeCoefficients aCoeff) : this(aCoeff, kMaximumDepth) { }
        public WorldTree(kdTreeCoefficients aCoeff, int aDepth) : base(aCoeff, aDepth) { }

        public void Collide(Body a, WorldBody b, ref Arbiter arArbiter)
        {
            if (a is IConvex)
            {
                OrientedBoundingBox obb;
                obb.Center = a.mFrame.Transform(Utilities.GetCenter(a.mLocalAABB));
                Vector3 halfExtents = Utilities.GetHalfExtents(a.mLocalAABB);
                obb.Rmag = new Vector3(a.mFrame.Orientation.M11, a.mFrame.Orientation.M12, a.mFrame.Orientation.M13) * halfExtents.X;
                obb.Smag = new Vector3(a.mFrame.Orientation.M21, a.mFrame.Orientation.M22, a.mFrame.Orientation.M23) * halfExtents.Y;
                obb.Tmag = new Vector3(a.mFrame.Orientation.M31, a.mFrame.Orientation.M32, a.mFrame.Orientation.M33) * halfExtents.Z;

                for (int i = 0; i < mNodeCount; )
                {
                    bool bIntersects = obb.Intersects(mNodes[i].AABB);
                    if (bIntersects)
                    {
                        List<Triangle> list = mNodes[i].Objects;
                        int count = list.Count;
                        for (int j = 0; j < count; j++)
                        {
                            WorldContactPoint wp;
                            Triangle t = list[j];
                            if (obb.Intersects(t.Box))
                            {
                                if (XenoCollide.Collide((IConvex)a, t, out wp))
                                {
                                    arArbiter.Add(new ContactPoint(a, b, wp));
                                }
                            }
                        }
                    }

                    i = _Next(bIntersects, i);
                }
            }
        }
    }
}
