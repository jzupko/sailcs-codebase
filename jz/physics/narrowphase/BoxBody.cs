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
using siat;
using jz.physics.broadphase;

namespace jz.physics.narrowphase
{
    public class BoxBody : RigidBody
    {
        #region Overrides
        protected override void _CalculateInertiaTensor()
        {
            if (Utilities.AboutZero(InverseMass))
            {
                mInertiaTensor = Matrix3.Zero;
                mInverseInertiaTensor = Matrix3.Zero;
            }
            else
            {
                const float kFactor = (float)(1.0 / 12.0);
                Vector3 extents = Utilities.GetExtents(ref mLocalAABB);

                float m = (!Utilities.AboutZero(InverseMass)) ? (1.0f / InverseMass) : 0.0f;
                mInertiaTensor.M11 = (m * (extents.Y * extents.Y + extents.Z * extents.Z) * kFactor);
                mInertiaTensor.M22 = (m * (extents.X * extents.X + extents.Z * extents.Z) * kFactor);
                mInertiaTensor.M33 = (m * (extents.X * extents.X + extents.Y * extents.Y) * kFactor);
                mInverseInertiaTensor = Matrix3.Invert(mInertiaTensor);
            }
        }

        public override Vector3 GetWorldSupport(Vector3 aWorldNormal)
        {
            Vector3 n = CoordinateFrame.Invert(mFrame).TransformNormal(aWorldNormal);
            Vector3 ret;

            if (n.X < 0.0f) { ret.X = mLocalAABB.Min.X; } else { ret.X = mLocalAABB.Max.X; }
            if (n.Y < 0.0f) { ret.Y = mLocalAABB.Min.Y; } else { ret.Y = mLocalAABB.Max.Y; }
            if (n.Z < 0.0f) { ret.Z = mLocalAABB.Min.Z; } else { ret.Z = mLocalAABB.Max.Z; }

            ret = mFrame.Transform(ret);

            return ret;
        }
        #endregion

        public BoxBody(ref BoundingBox aBox)
            : base(BodyFlags.kStatic, BodyFlags.kDynamic)
        {
            mLocalAABB = aBox;

            _CalculateInertiaTensor();
        }
    }
}
