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
using System.Diagnostics;
using System.Text;
using siat;

namespace jz.physics.narrowphase
{
    /// <summary>
    /// Manages a persistent set of contact points between two rigid bodies.
    /// </summary>
    /// <remarks>
    /// Based on btPersistentManifold of Bullet Physics: http://www.bulletphysics.com/Bullet/wordpress/
    /// </remarks>
    public struct Arbiter
    {
        public const float kConcaveNormalTolerance = 0.9f;
        public const int kConvexMaxPoints = 4;

        #region Private members
        private void _ReduceConvex(ContactPoint aPoint)
        {
            Debug.Assert(Contacts.Count == kConvexMaxPoints);

            int maxPenetrationIndex = -1;
            float maxPenetration = aPoint.GetSeparationSquared(A, B);

            for (int i = 0; i < kConvexMaxPoints; i++)
            {
                float d = Contacts[i].GetSeparationSquared(A, B);
                if (d > maxPenetration) 
                {
                    maxPenetrationIndex = i;
                    maxPenetration = d;
                }
            }
		
            Vector4 res = Vector4.Zero;
    		if (maxPenetrationIndex != 0)
		    {
			    Vector3 v0 = aPoint.LocalPointA - Contacts[1].LocalPointA;
			    Vector3 v1 = Contacts[3].LocalPointA - Contacts[2].LocalPointA;
			    Vector3 c = Vector3.Cross(v0, v1);
                res.X = c.LengthSquared();
		    }

		    if (maxPenetrationIndex != 1)
		    {
			    Vector3 v0 = aPoint.LocalPointA - Contacts[0].LocalPointA;
			    Vector3 v1 = Contacts[3].LocalPointA - Contacts[2].LocalPointA;
			    Vector3 c = Vector3.Cross(v0, v1);
                res.Y = c.LengthSquared();
		    }

		    if (maxPenetrationIndex != 2)
		    {
			    Vector3 v0 = aPoint.LocalPointA - Contacts[0].LocalPointA;
			    Vector3 v1 = Contacts[3].LocalPointA - Contacts[1].LocalPointA;
			    Vector3 c = Vector3.Cross(v0, v1);
                res.Z = c.LengthSquared();
 		    }

		    if (maxPenetrationIndex != 3)
		    {
			    Vector3 v0 = aPoint.LocalPointA - Contacts[0].LocalPointA;
			    Vector3 v1 = Contacts[2].LocalPointA - Contacts[1].LocalPointA;
			    Vector3 c = Vector3.Cross(v0, v1);
                res.W = c.LengthSquared();
		    }

            int index = (int)Utilities.GetClosestAxis(ref res);

            ContactPoint p = Contacts[index];
            p.LocalPointA = aPoint.LocalPointA;
            p.LocalPointB = aPoint.LocalPointB;
            p.WorldNormal = aPoint.WorldNormal;
            Contacts[index] = p;
        }

        private void _RefreshConvex()
        {
            int count = Contacts.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 wa = A.Frame.Transform(Contacts[i].LocalPointA);
                Vector3 wb = B.Frame.Transform(Contacts[i].LocalPointB);
                Vector3 wn = Contacts[i].WorldNormal;

                Vector3 d = (wa - wb);
                if (Vector3.Dot(d, wn) > PhysicsConstants.kMinimumThickness)
                {
                    Contacts.RemoveAt(i); i--; count--;
                }
                else
                {
                    Vector3 t = (d - (Vector3.Dot(d, wn) * wn));
                    if (Vector3.Dot(t, t) > PhysicsConstants.kMinimumThickness2)
                    {
                        Contacts.RemoveAt(i); i--; count--;
                    }
                }
            }
        }
        #endregion

        public bool bConcave;
        public Body A;
        public Body B;
        public List<ContactPoint> Contacts;
        public int SortOrder;

        public void Add(ContactPoint ap)
        {
            if (!bConcave)
            {
                _RefreshConvex();

                int count = Contacts.Count;

                Debug.Assert(count <= kConvexMaxPoints);
                if (count == kConvexMaxPoints) { _ReduceConvex(ap); }
                else { Contacts.Add(ap); }
            }
            else
            {
                Contacts.Add(ap);
            }
        }
    }

    public struct ContactEntry
    {
        public ContactEntry(ContactPair aPair, WorldContactPoint aPoint)
        {
            Pair = aPair;
            Point = aPoint;
        }

        public ContactPair Pair;
        public WorldContactPoint Point;
    }

    public struct ContactPair : IEquatable<ContactPair>
    {
        public ContactPair(Body a, Body b)
        {
            A = a;
            B = b;
            SortOrder = 0;
        }

        public override bool Equals(object b)
        {
            if (b is ContactPair)
            {
                return (this == ((ContactPair)b));
            }
            else { return false; }
        }

        public bool Equals(ContactPair b)
        {
            return (this == b);
        }

        public override int GetHashCode()
        {
            return (A.GetHashCode() | B.GetHashCode());
        }

        public static bool operator ==(ContactPair a, ContactPair b)
        {
            return (a.A == b.A && a.B == b.B);
        }

        public static bool operator !=(ContactPair a, ContactPair b)
        {
            return (a.A != b.A || a.B != b.B);
        }

        public Body A;
        public Body B;
        public int SortOrder;
    }

    /// <summary>
    /// Contact point between two rigid bodies.
    /// </summary>
    /// <remarks>
    /// Dynamics code based on Catto, E. 2006. "Fast and Simple Physics using Sequential Impulses", 
    ///     GDC 2006: http://www.gphysics.com/downloads/
    /// </remarks>
    public struct ContactPoint
    {
        #region Private members
        private float mMass;
        private float mMomentum;
        private float mPositionError;
        #endregion

        #region Internal members
        internal void _Pre(Body a, Body b)
        {
            Vector3 wa = a.mFrame.Transform(LocalPointA);
            Vector3 wb = b.mFrame.Transform(LocalPointB);
            Vector3 wn = WorldNormal;

            Vector3 ra = (wa - a.mFrame.Translation);
            Vector3 rb = (wb - b.mFrame.Translation);

            mPositionError = Utilities.Max(PhysicsConstants.kBeta * (Vector3.Dot((wb - wa), wn) - PhysicsConstants.kShift), 0.0f);

            Vector3 ima = Vector3.Zero;
            Vector3 imb = Vector3.Zero;

            float invMa = 0.0f;
            float invMb = 0.0f;

            if (a is RigidBody)
            {
                RigidBody rba = (RigidBody)a;
                ima = rba.mInverseInertiaTensor.Transform(Vector3.Cross(ra, wn));
                invMa = rba.InverseMass;
            }

            if (b is RigidBody)
            {
                RigidBody rbb = (RigidBody)b;
                imb = rbb.mInverseInertiaTensor.Transform(Vector3.Cross(rb, wn));
                invMb = rbb.InverseMass;
            }

            mMass = 1.0f / (invMa + invMb + Vector3.Dot(wn, (Vector3.Cross(ima, ra) + Vector3.Cross(imb, rb))));

            if (a is RigidBody)
            {
                RigidBody rba = (RigidBody)a;
                rba.mLinearMomentum -= mMomentum * wn;
                rba.mAngularMomentum -= mMomentum * Vector3.Cross(ra, wn);
            }

            if (b is RigidBody)
            {
                RigidBody rbb = (RigidBody)b;
                rbb.mLinearMomentum += mMomentum * wn;
                rbb.mAngularMomentum += mMomentum * Vector3.Cross(rb, wn);
            }
        }

        internal float _Tick(Body a, Body b)
        {
            float ret = 0.0f;

            Vector3 wa = a.mFrame.Transform(LocalPointA);
            Vector3 wb = b.mFrame.Transform(LocalPointB);
            Vector3 wn = WorldNormal;

            Vector3 ra = (wa - a.mFrame.Translation);
            Vector3 rb = (wb - b.mFrame.Translation);

            #region Normal Impulse
            {
                Vector3 la = Vector3.Zero;
                Vector3 lb = Vector3.Zero;
                Vector3 aa = Vector3.Zero;
                Vector3 ab = Vector3.Zero;

                if (a is RigidBody)
                {
                    RigidBody rba = (RigidBody)a;
                    la = (rba.InverseMass * rba.mLinearMomentum);
                    aa = rba.mInverseInertiaTensor.Transform(Vector3.Cross(rba.mAngularMomentum, ra));
                }

                if (b is RigidBody)
                {
                    RigidBody rbb = (RigidBody)b;
                    lb = (rbb.InverseMass * rbb.mLinearMomentum);
                    ab = rbb.mInverseInertiaTensor.Transform(Vector3.Cross(rbb.mAngularMomentum, rb));
                }

                Vector3 rv = (lb + ab) - (la + aa);

                float ve = Vector3.Dot(rv, wn);
                float dv = -(ve + mPositionError);
                float mp = (dv * mMass);
                mp = Utilities.Min(mp, -mMomentum);
                mMomentum += mp;
                ret += Math.Abs(mp);
                Vector3 mpv = (mp * wn);

                if (a is RigidBody)
                {
                    RigidBody rba = (RigidBody)a;
                    rba.mLinearMomentum -= mpv;
                    rba.mAngularMomentum -= (mp * Vector3.Cross(ra, wn));
                }

                if (b is RigidBody)
                {
                    RigidBody rbb = (RigidBody)b;
                    rbb.mLinearMomentum += mpv;
                    rbb.mAngularMomentum += (mp * Vector3.Cross(rb, wn));
                }
            }
            #endregion

            #region Tangent Impulse
            {
                Vector3 la = Vector3.Zero;
                Vector3 lb = Vector3.Zero;
                Vector3 aa = Vector3.Zero;
                Vector3 ab = Vector3.Zero;

                if (a is RigidBody)
                {
                    RigidBody rba = (RigidBody)a;
                    la = (rba.InverseMass * rba.mLinearMomentum);
                    aa = rba.mInverseInertiaTensor.Transform(Vector3.Cross(rba.mAngularMomentum, ra));
                }

                if (b is RigidBody)
                {
                    RigidBody rbb = (RigidBody)b;
                    lb = (rbb.InverseMass * rbb.mLinearMomentum);
                    ab = rbb.mInverseInertiaTensor.Transform(Vector3.Cross(rbb.mAngularMomentum, rb));
                }

                Vector3 rv = (lb + ab) - (la + aa);
                Vector3 vn = Utilities.SafeNormalize((rv - (Vector3.Dot(rv, wn) * wn)));

                Vector3 ima = Vector3.Zero;
                Vector3 imb = Vector3.Zero;
                float invMa = 0.0f;
                float invMb = 0.0f;

                if (a is RigidBody)
                {
                    RigidBody rba = (RigidBody)a;
                    ima = rba.mInverseInertiaTensor.Transform(Vector3.Cross(ra, vn));
                    invMa = rba.InverseMass;
                }

                if (b is RigidBody)
                {
                    RigidBody rbb = (RigidBody)b;
                    imb = rbb.mInverseInertiaTensor.Transform(Vector3.Cross(rb, vn));
                    invMb = rbb.InverseMass;
                }
                
                float m = 1.0f / (invMa + invMb + Vector3.Dot(vn, (Vector3.Cross(ima, ra) + Vector3.Cross(imb, rb))));
                float e = (Vector3.Dot(rv, vn));
                float mp = (-e * m);
                mp = Utilities.Min(mp, -mMomentum);
                ret += Math.Abs(mp);
                Vector3 mpv = (mp * vn);

                if (a is RigidBody)
                {
                    RigidBody rba = (RigidBody)a;
                    rba.mLinearMomentum -= (rba.Friction * mpv);
                    rba.mAngularMomentum -= (rba.Friction * mp * Vector3.Cross(ra, vn));
                }

                if (b is RigidBody)
                {
                    RigidBody rbb = (RigidBody)b;
                    rbb.mLinearMomentum += (rbb.Friction * mpv);
                    rbb.mAngularMomentum += (rbb.Friction * mp * Vector3.Cross(rb, vn));
                }
            }
            #endregion

            return ret;
        }
        #endregion

        public ContactPoint(Body a, Body b, WorldContactPoint aWorld)
        {
            mMass = 0.0f;
            mMomentum = 0.0f;
            mPositionError = 0.0f;

            LocalPointA = CoordinateFrame.Invert(a.mFrame).Transform(aWorld.WorldPointA);
            LocalPointB = CoordinateFrame.Invert(b.mFrame).Transform(aWorld.WorldPointB);
            WorldNormal = aWorld.WorldNormal;
        }

        public float GetSeparationSquared(Body a, Body b)
        {
            Vector3 wa = a.mFrame.Transform(LocalPointA);
            Vector3 wb = b.mFrame.Transform(LocalPointB);

            float ret = Vector3.DistanceSquared(wa, wb);

            return ret;
        }

        public ContactPoint Flip()
        {
            ContactPoint ret = this;
            Utilities.Swap(ref ret.LocalPointA, ref ret.LocalPointB);
            ret.WorldNormal = -ret.WorldNormal;

            return ret;
        }

        public Vector3 LocalPointA;
        public Vector3 LocalPointB;
        public Vector3 WorldNormal;
    }

    public struct WorldContactPoint
    {
        public const float kNormalTolerance = 0.9f;

        public Vector3 WorldNormal;
        public Vector3 WorldPointA;
        public Vector3 WorldPointB;

        public bool AboutEqual(WorldContactPoint b)
        {
            Vector3 ac = 0.5f * (WorldPointA + WorldPointB);
            Vector3 bc = 0.5f * (b.WorldPointA + b.WorldPointB);

            bool bReturn = (Vector3.DistanceSquared(ac, bc) < PhysicsConstants.kMinimumThickness2);

            return bReturn;
        }

        public float SeparationSquared
        {
            get
            {
                return (Vector3.DistanceSquared(WorldPointA, WorldPointB));
            }
        }
    }
}
