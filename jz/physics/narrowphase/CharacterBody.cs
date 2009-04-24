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
    public class CharacterBody : RigidBody
    {
        #region Protected members
        protected bool mbDisableUp = false;

        protected float mRadius;
        protected float mHalfHeight;
        protected Vector3 mCenter;
        #endregion

        #region Overrides
        protected override void _CalculateInertiaTensor()
        {
            if (Utilities.AboutZero(InverseMass))
            {
                mInertiaTensor = Matrix3.Zero;
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

        /// <summary>
        /// Character body as a cylinder.
        /// </summary>
        /// <param name="aWorldNormal"></param>
        /// <returns></returns>
        public override Vector3 GetWorldSupport(Vector3 aWorldNormal)
        {
            Vector3 n = CoordinateFrame.Invert(mFrame).TransformNormal(aWorldNormal);
            Vector3 n2 = Utilities.SafeNormalize(new Vector3(n.X, 0.0f, n.Z));

            Vector3 ret = mCenter + new Vector3(n2.X * mRadius, Math.Sign(n.Y) * (mHalfHeight + mRadius), n2.Z * mRadius);

            ret = mFrame.Transform(ret);

            return ret;
        }
        #endregion

        public CharacterBody(float aRadius, float aHeight, Vector3 aCenter)
            : base(BodyFlags.kStatic, BodyFlags.kDynamic)
        {
            mRadius = aRadius;
            mHalfHeight = 0.5f * aHeight;
            mCenter = aCenter;

            Vector3 min = new Vector3(-mRadius, -mHalfHeight - mRadius, -mRadius) + mCenter;
            Vector3 max = new Vector3(mRadius, mHalfHeight + mRadius, mRadius) + mCenter;

            mLocalAABB = new BoundingBox(min, max);
            _CalculateInertiaTensor();
        }

        public bool bDisableUp { get { return mbDisableUp; } set { mbDisableUp = value; } }

        public override void Apply(Body b, ContactPoint aPoint)
        {
            Vector3 wa = mFrame.Transform(aPoint.LocalPointA);
            Vector3 wb = b.mFrame.Transform(aPoint.LocalPointB);
            Vector3 wn = aPoint.WorldNormal;

            if (mbDisableUp) { wn = Utilities.SafeNormalize(wn - (Vector3.Dot(wn, Vector3.Up) * Vector3.Up)); }

            Vector3 diff = Vector3.Dot(wb - wa, wn) * wn;
            Vector3 frameDiff = Vector3.Dot(mFrame.Translation - mPrevFrame.Translation, wn) * wn;

            if (Vector3.Dot(diff, wn) > 0.0f &&
                Vector3.Dot((wa + diff) - mFrame.Translation, wn) < 0.0f)
            {
                mFrame.Translation += diff;
            }
        }
    }
}
