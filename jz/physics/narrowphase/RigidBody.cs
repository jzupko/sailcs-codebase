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
using siat;
using jz.physics.broadphase;

namespace jz.physics.narrowphase
{
    /// <summary>
    /// Base class for collideable bodies.
    /// </summary>
    public abstract class RigidBody : Body, IConvex
    {
        #region Private members
        private float mFriction = PhysicsConstants.kDefaultFriction;
        private float mInverseMass = 0.0f;
        #endregion

        #region Protected members
        protected abstract void _CalculateInertiaTensor();

        protected RigidBody(BodyFlags aType, BodyFlags aCollidesWith)
            : base(aType, aCollidesWith)
        { }
        #endregion

        #region Internal members
        internal Vector3 mAngularMomentum = Vector3.Zero;
        internal Matrix3 mInertiaTensor = Matrix3.Zero;
        internal Matrix3 mInverseInertiaTensor = Matrix3.Zero;
        internal Vector3 mLinearMomentum = Vector3.Zero;

        internal void _Integrate()
        {
            if (Type == BodyFlags.kDynamic)
            {
                float m = (1.0f / mInverseMass);

                mLinearMomentum += (World.Gravity * m * PhysicsConstants.kTimeStep);
                Vector3 lin = (mLinearMomentum * mInverseMass);
                Vector3 ang = (mInverseInertiaTensor.Transform(mAngularMomentum));

                PhysicsUtilities.GetFrame(ref mFrame, ref lin, ref ang, PhysicsConstants.kTimeStep, ref mFrame);
                _UpdateWorldAABB();
            }
        }
        #endregion

        public Vector3 AngularMomentum { get { return mAngularMomentum; } }
        public float Friction { get { return mFriction; } set { mFriction = Utilities.Clamp(value, PhysicsConstants.kMinimumFriction, PhysicsConstants.kMaximumFriction); } }
        public abstract Vector3 GetWorldSupport(Vector3 aWorldNormal);
        public Vector3 LinearMomentum { get { return mLinearMomentum; } }
        public Vector3 WorldTranslation { get { return mFrame.Translation; } }

        public float InverseMass
        {
            get { return mInverseMass; }
            set
            {
                if (Type == BodyFlags.kDynamic) { mInverseMass = Utilities.Max(Utilities.kLooseToleranceFloat, value); }
                else { mInverseMass = 0.0f; }

                _CalculateInertiaTensor();
            }
        }

        public override BodyFlags Type
        {
            get { return base.Type; }
            set
            {
                if (value != Type)
                {
                    base.Type = value;
                    InverseMass = (value == BodyFlags.kDynamic) ? (1.0f / PhysicsConstants.kDefaultMass) : 0.0f;
                }
            }
        }
    }
}
