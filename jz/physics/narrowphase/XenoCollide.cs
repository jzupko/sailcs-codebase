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
using jz.physics.broadphase;

namespace jz.physics.narrowphase
{
    /// <summary>
    /// Collision of convex shapes.
    /// </summary>
    /// <remarks>
    /// C# implementation of Gary Snethen's XenoCollide: http://xenocollide.snethen.com
    /// </remarks>
    public static class XenoCollide
    {
        #region Private members
        private static bool _Collide(IConvex a, IConvex b, out WorldContactPoint arContactPoint)
        {
            arContactPoint = new WorldContactPoint();

            Vector3 v01 = a.WorldTranslation;
            Vector3 v02 = b.WorldTranslation;
            Vector3 v0 = (v02 - v01);

            if (Utilities.AboutZero(ref v0)) { v0 = Vector3.Forward * Utilities.kLooseToleranceFloat; }

            Vector3 normal = -v0;
            Vector3 v11 = a.GetWorldSupport(-normal);
            Vector3 v12 = b.GetWorldSupport(normal);
            Vector3 v1 = (v12 - v11);
            float dot = Vector3.Dot(v1, normal);

            if (dot <= 0.0f)
            {
                arContactPoint.WorldNormal = Vector3.Normalize(normal);
                arContactPoint.WorldPointA = Vector3.Zero;
                arContactPoint.WorldPointB = Vector3.Zero;
                return false;
            }

            normal = Vector3.Cross(v1, v0);

            if (Utilities.AboutZero(ref normal))
            {
                normal = Vector3.Normalize(v1 - v0);

                arContactPoint.WorldNormal = normal;
                arContactPoint.WorldPointA = v11;
                arContactPoint.WorldPointB = v12;
                return true;
            }

            Vector3 v21 = a.GetWorldSupport(-normal);
            Vector3 v22 = b.GetWorldSupport(normal);
            Vector3 v2 = (v22 - v21);
            dot = Vector3.Dot(v2, normal);

            if (dot <= 0.0f)
            {
                arContactPoint.WorldNormal = normal;
                arContactPoint.WorldPointA = Vector3.Zero;
                arContactPoint.WorldPointB = Vector3.Zero;
                return false;
            }

            normal = Vector3.Cross((v1 - v0), (v2 - v0));
            float distance = Vector3.Dot(normal, v0);

            if (distance > 0.0f)
            {
                Utilities.Swap(ref v1, ref v2);
                Utilities.Swap(ref v11, ref v21);
                Utilities.Swap(ref v12, ref v22);
                normal = -normal;
            }

            // identify a portal
            while (true)
            {
                Vector3 v31 = a.GetWorldSupport(-normal);
                Vector3 v32 = b.GetWorldSupport(normal);
                Vector3 v3 = (v32 - v31);
                dot = Vector3.Dot(v3, normal);

                if (dot <= 0.0f)
                {
                    arContactPoint.WorldNormal = Vector3.Normalize(normal);
                    arContactPoint.WorldPointA = Vector3.Zero;
                    arContactPoint.WorldPointB = Vector3.Zero;
                    return false;
                }

                // origin is outside (v1, v0, v3), eliminate v2 and loop
                if (Vector3.Dot(Vector3.Cross(v1, v3), v0) < 0.0f)
                {
                    v2 = v3;
                    v21 = v31;
                    v22 = v32;
                    normal = Vector3.Cross((v1 - v0), (v3 - v0));
                    continue;
                }

                // origin is outside (v3, v0, v2), eliminate v1 and loop
                if (Vector3.Dot(Vector3.Cross(v3, v2), v0) < 0.0f)
                {
                    v1 = v3;
                    v11 = v31;
                    v12 = v32;
                    normal = Vector3.Cross((v3 - v0), (v2 - v0));
                    continue;
                }

                bool bDone = false;

                // refine the portal.
                while (true)
                {
                    normal = Vector3.Normalize(Vector3.Cross((v2 - v1), (v3 - v1)));
                    dot = Vector3.Dot(normal, v1);

                    if (dot >= 0.0f && !bDone)
                    {
                        float b0 = Vector3.Dot(Vector3.Cross(v1, v2), v3);
                        float b1 = Vector3.Dot(Vector3.Cross(v3, v2), v0);
                        float b2 = Vector3.Dot(Vector3.Cross(v0, v1), v3);
                        float b3 = Vector3.Dot(Vector3.Cross(v2, v1), v0);

                        float sum = (b0 + b1 + b2 + b3);

                        if (sum <= 0.0f)
                        {
                            b0 = 0.0f;
                            b1 = Vector3.Dot(Vector3.Cross(v2, v3), normal);
                            b2 = Vector3.Dot(Vector3.Cross(v3, v1), normal);
                            b3 = Vector3.Dot(Vector3.Cross(v1, v2), normal);

                            sum = (b1 + b2 + b3);
                        }

                        float inv = (1.0f / sum);

                        Vector3 wa = ((b0 * v01) + (b1 * v11) + (b2 * v21) + (b3 * v31)) * inv;
                        Vector3 wb = ((b0 * v02) + (b1 * v12) + (b2 * v22) + (b3 * v32)) * inv;

                        arContactPoint.WorldNormal = normal;
                        arContactPoint.WorldPointA = wa;
                        arContactPoint.WorldPointB = wb;
                        bDone = true;
                    }

                    Vector3 v41 = a.GetWorldSupport(-normal);
                    Vector3 v42 = b.GetWorldSupport(normal);
                    Vector3 v4 = (v42 - v41);

                    float delta = Vector3.Dot((v4 - v3), normal);
                    float separation = -Vector3.Dot(v4, normal);

                    if (delta <= Utilities.kLooseToleranceFloat || separation >= 0.0f)
                    {
                        arContactPoint.WorldNormal = normal;
                        return bDone;
                    }

                    float d1 = Vector3.Dot(Vector3.Cross(v4, v1), v0);
                    float d2 = Vector3.Dot(Vector3.Cross(v4, v2), v0);
                    float d3 = Vector3.Dot(Vector3.Cross(v4, v3), v0);

                    if (d1 < 0.0f)
                    {
                        if (d2 < 0.0f)
                        {
                            v1 = v4;
                            v11 = v41;
                            v12 = v42;
                        }
                        else
                        {
                            v3 = v4;
                            v31 = v41;
                            v32 = v42;
                        }
                    }
                    else
                    {
                        if (d3 < 0.0f)
                        {
                            v2 = v4;
                            v21 = v41;
                            v22 = v42;
                        }
                        else
                        {
                            v1 = v4;
                            v11 = v41;
                            v12 = v42;
                        }
                    }
                }
            }
        }
        #endregion
                
        public static bool Collide(IConvex a, IConvex b, out WorldContactPoint arContactPoint)
        {
            bool bReturn = _Collide(a, b, out arContactPoint);

            if (bReturn)
            {
                Vector3 wa = arContactPoint.WorldPointA;
                Vector3 wb = arContactPoint.WorldPointB;
                Vector3 wn = arContactPoint.WorldNormal;

                Vector3 s1 = a.GetWorldSupport(-wn);
                Vector3 s2 = b.GetWorldSupport(wn);

                Vector3 ppA = (Vector3.Dot((s1 - wa), wn) * wn) + wa;
                Vector3 ppB = (Vector3.Dot((s2 - wb), wn) * wn) + wb;

                arContactPoint.WorldPointA = ppA;
                arContactPoint.WorldPointB = ppB;
            }

            return bReturn;
        }

    }
}
