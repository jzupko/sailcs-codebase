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

using jz.physics.broadphase;
using jz.physics.narrowphase;
using siat;

namespace jz.physics
{
    public sealed class World
    {
        #region Private members
        private IBroadphase mBroadphase;
        private List<RigidBody> mDynamics = new List<RigidBody>();
        private List<Body> mKinematics = new List<Body>();
        private List<Body> mStatics = new List<Body>();
        private Vector3 mGravity = PhysicsConstants.kDefaultGravity;
        private float mTimePool = 0.0f;
        #endregion

        #region Internal members
        internal void Add(Body aBody)
        {
            if (aBody.Type == BodyFlags.kDynamic) { mDynamics.Add((RigidBody)aBody); }
            else if (aBody.Type == BodyFlags.kKinematic) { mKinematics.Add(aBody); }
            else { mStatics.Add(aBody); }

            BoundingBox aabb = aBody.LocalAABB;
            CoordinateFrame frame = aBody.Frame;
            CoordinateFrame.Transform(ref aabb, ref frame, out aabb);
            aBody.mHandle = mBroadphase.Add(aBody, ref aabb);
        }

        internal void Remove(Body aBody)
        {
            mBroadphase.Remove(aBody.mHandle);
            aBody.mHandle = ushort.MaxValue;

            if (aBody is RigidBody) { mDynamics.Remove((RigidBody)aBody); }
            mKinematics.Remove(aBody);
            mStatics.Remove(aBody);
        }

        internal void Update(Body aBody, ref BoundingBox aAABB)
        {
            mBroadphase.Update(aBody.mHandle, ref aAABB);
        }

        internal void UpdateType(Body aBody)
        {
            if (aBody is RigidBody) { mDynamics.Remove((RigidBody)aBody); }
            mKinematics.Remove(aBody);
            mStatics.Remove(aBody);

            if (aBody.Type == BodyFlags.kDynamic) { mDynamics.Add((RigidBody)aBody); }
            else if (aBody.Type == BodyFlags.kKinematic) { mKinematics.Add(aBody); }
            else { mStatics.Add(aBody); }
        }
        #endregion

        public World() : this(null) { }
        public World(IBroadphase aBroadphase)
        {
            mBroadphase = (aBroadphase == null) ? new Sap() : aBroadphase;
        }

        public Vector3 Gravity { get { return mGravity; } set { mGravity = value; } }

        public void Tick(float aTimeStep)
        {
            mTimePool += aTimeStep;

            int iteration = 0;
            while (Utilities.GreaterThan(mTimePool, PhysicsConstants.kTimeStep))
            {
                mTimePool -= PhysicsConstants.kTimeStep;

                #region Integrate
                {
                    int count = mDynamics.Count;
                    for (int i = 0; i < count; i++) { mDynamics[i]._Integrate(); }
                }
                #endregion

                #region Broadphase
                List<Arbiter> arbiters = new List<Arbiter>();
                mBroadphase.Tick(arbiters);
                #endregion

                #region Narrow phase
                if (arbiters.Count > 0)
                {
                    Random r = new Random();
                    List<Arbiter> ps = arbiters;
                    int count = ps.Count;

                    #region Sort
                    for (int i = 0; i < count; i++) { Arbiter pair = ps[i]; pair.SortOrder = r.Next(); ps[i] = pair; }
                    ps.Sort(delegate(Arbiter a, Arbiter b) { return a.SortOrder.CompareTo(b.SortOrder); });
                    #endregion

                    #region Collision
                    for (int i = 0; i < count; i++)
                    {
                        if (ps[i].A is IConvex && ps[i].B is IConvex)
                        {
                            WorldContactPoint wp;
                            if (XenoCollide.Collide((IConvex)(ps[i].A), (IConvex)(ps[i].B), out wp))
                            {
                                ContactPoint point = new ContactPoint(ps[i].A, ps[i].B, wp);

                                Arbiter a = ps[i];
                                a.Add(point);
                                ps[i] = a;
                            }
                        }
                        else if (ps[i].B is WorldBody)
                        {
                            if (ps[i].A.Type == BodyFlags.kDynamic || iteration == 0)
                            {
                                WorldBody wb = (WorldBody)ps[i].B;
                                Arbiter a = ps[i];
                                a.bConcave = true;
                                a.Contacts.Clear();

                                wb.WorldTree.Collide(ps[i].A, wb, ref a);
                                
                                ps[i] = a;
                            }
                        }
                        else if (ps[i].A is WorldBody)
                        {
                            if (ps[i].B.Type == BodyFlags.kDynamic || iteration == 0)
                            {
                                WorldBody wb = (WorldBody)ps[i].A;
                                Arbiter a = ps[i];
                                a.bConcave = true;
                                a.Contacts.Clear();

                                wb.WorldTree.Collide(ps[i].B, wb, ref a);
                                
                                for (int j = 0; j < a.Contacts.Count; j++) { a.Contacts[j] = a.Contacts[j].Flip(); }
                                ps[i] = a;
                            }
                        }
                    }
                    #endregion

                    #region Pre
                    for (int i = 0; i < count; i++)
                    {
                        if (ps[i].A.Type == BodyFlags.kDynamic || ps[i].B.Type == BodyFlags.kDynamic)
                        {
                            int subCount = ps[i].Contacts.Count;
                            for (int j = 0; j < subCount; j++)
                            {
                                ContactPoint point = ps[i].Contacts[j];
                                point._Pre(ps[i].A, ps[i].B);
                                ps[i].Contacts[j] = point;
                            }
                        }

                        if (ps[i].A.Type == BodyFlags.kDynamic ||
                            ps[i].B.Type == BodyFlags.kDynamic ||
                            iteration == 0)
                        {
                            int subCount = ps[i].Contacts.Count;
                            for (int j = 0; j < subCount; j++)
                            {
                                ContactPoint point = ps[i].Contacts[j];
                                ContactPoint flipped = point.Flip();

                                ps[i].A.Apply(ps[i].B, point);
                                ps[i].B.Apply(ps[i].A, flipped);
                            }
                        }
                    }
                    #endregion

                    #region Iterate
                    for (int k = 0; k < PhysicsConstants.kMaxSolverIterations; k++)
                    {
                        float error = 0.0f;
                        for (int i = 0; i < count; i++)
                        {
                            if (ps[i].A.Type == BodyFlags.kDynamic || ps[i].B.Type == BodyFlags.kDynamic)
                            {
                                int subCount = ps[i].Contacts.Count;
                                for (int j = 0; j < subCount; j++)
                                {
                                    ContactPoint point = ps[i].Contacts[j];
                                    error += point._Tick(ps[i].A, ps[i].B);
                                    ps[i].Contacts[j] = point;
                                }
                            }
                        }

                        if (error < PhysicsConstants.kDesiredError) { break; }
                    }
                    #endregion
                }
                #endregion

                iteration++;
            }
        }
    }
}
