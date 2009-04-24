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
using jz.physics.narrowphase;

namespace jz.physics
{
    public static class PhysicsUtilities
    {
        #region Intersection
        /// <summary>
        /// Intersection of two swept spheres based on linear velocity a and b and a time step.
        /// </summary>
        /// <param name="aSphereA">Sphere A.</param>
        /// <param name="aSphereB">Sphere B.</param>
        /// <param name="aLinearA">Linear velocity of A.</param>
        /// <param name="aLinearB">Linear velocity of B.</param>
        /// <param name="aTimeStep">Time during which to check for intersection.</param>
        /// <returns>Time t of intersection.</returns>
        /// <remarks>
        /// The returned time t is relative to the t0, which is the time of the starting positions of
        /// the two spheres. Therefore, an intersection occurs during the time step if t is <= aTimeStep.
        /// 
        /// From: Lengyel, E. 2004. "Mathematics for 3D Game Programming & Computer Graphics", 
        ///     Charles River Media, Inc. ISBN: 1-58450-277-0
        /// </remarks>
        public static bool SweptIntersect(ref BoundingSphere aSphereA, ref BoundingSphere aSphereB,
            ref Vector3 aLinearA, ref Vector3 aLinearB, float aTimeStep, 
            ref float arTimeIntersect, ref Vector3 arTranslationIntersect)
        {
            Vector3 a = (aSphereA.Center - aSphereB.Center);
            Vector3 b = (aLinearA - aLinearB);

            float radiusSum = (aSphereA.Radius + aSphereB.Radius);
            float radiusSumSquare = (radiusSum * radiusSum);

            float aSquare = a.LengthSquared();

            // Spheres are already intersecting, calculate a point of penetration.
            if (radiusSumSquare < aSquare)
            {
                arTimeIntersect = 0.0f;
                arTranslationIntersect = aSphereB.Center + (0.5f * a);
                return true;
            }
            // Else, if b is about zero, then the spheres are moving away or not moving and cannot
            // intersect.
            else if (Utilities.AboutZero(ref b)) { return false; }

            float bSquare = b.LengthSquared();
            float invBSquare = 1.0f / bSquare;

            float dot;
            Vector3.Dot(ref a, ref b, out dot);
            float dotSquare = (dot * dot);

            // Relatively cheap test for early out - if the minDistanceSquare > radiusSumSquare,
            // then the spheres can never intersect during this time interval.
            float minDistanceSquare = (aSquare - (dotSquare * invBSquare ));
            if (minDistanceSquare > radiusSumSquare)
            {
                return false;
            }

            // Final, expensive test using quadratic formula for time t of intersection.
            arTimeIntersect = (-dot - (float)Math.Sqrt(dotSquare - (bSquare * (aSquare - radiusSumSquare)))) * invBSquare;
            PhysicsUtilities.GetTranslation(ref aSphereB.Center, ref aLinearB, arTimeIntersect, out arTranslationIntersect);

            return true;
        }
        #endregion

        #region Integrate a coordinate frame from angular and linear velocity.
        /// <summary>
        /// Calculates a predicted coordinate frame from a starting fram a, linear velocity, angular velocity, and time step.
        /// </summary>
        /// <param name="a">The coordinate frame a.</param>
        /// <param name="aLinear">The linear velocity.</param>
        /// <param name="aAngular">The angular velocity.</param>
        /// <param name="aTimeStep">Time step.</param>
        /// <param name="arB">The predicted frame.</param>
        public static void GetFrame(ref CoordinateFrame a,
            ref Vector3 aLinear, ref Vector3 aAngular, float aTimeStep,
            ref CoordinateFrame arB)
        {
            GetFrameLinear(ref a, ref aLinear, aTimeStep, ref arB);
            GetFrameAngular(ref a, ref aAngular, aTimeStep, ref arB);
        }

        /// <summary>
        /// Predicts the orientation part of a coordinate frame b from a coordinate frame a and an angular velocity.
        /// </summary>
        /// <param name="a">Coordinate frame a.</param>
        /// <param name="aAngular">The angular velocity.</param>
        /// <param name="aTimeStep">The time step.</param>
        /// <param name="arB">The predicted coordinate frame.</param>
        /// <remarks>
        /// Uses an "Exponential Map": Grassia, F. 1998. "Practical Parameterization of Rotations Using the Exponential Map"
        ///     The Journal of Graphics Toosl, 3(3).
        /// </remarks>
        public static void GetFrameAngular(ref CoordinateFrame a, ref Vector3 aAngular, float aTimeStep, ref CoordinateFrame arB)
        {
            const float kTaylorFactor = (float)(1.0 / 48.0);

            Radian angle = new Radian(aAngular.Length());
            if ((angle * aTimeStep) > PhysicsConstants.kMaximumAngle) { angle = (PhysicsConstants.kMaximumAngle / aTimeStep); }

            Vector3 axis = (angle < PhysicsConstants.kMinimumAngle)
                ? (aAngular * ((0.5f * aTimeStep) - ((aTimeStep * aTimeStep * aTimeStep) * kTaylorFactor * (angle * angle))))
                : (aAngular * (Radian.Sin(0.5f * angle * aTimeStep) / angle));

            Quaternion q = new Quaternion(axis, Radian.Cos(0.5f * angle * aTimeStep));
            Quaternion q0;
            Utilities.FromMatrix(ref a.Orientation, out q0);
            q0 = Quaternion.Normalize(Quaternion.Concatenate(q0, q));
            Utilities.ToMatrix(ref q0, out arB.Orientation);
        }

        public static void GetTranslation(ref Vector3 v0, ref Vector3 aLinearVelocity, float aTimeStep, out Vector3 v1)
        {
            v1 = (v0 + (aLinearVelocity * aTimeStep));
        }

        /// <summary>
        /// Predicts the translation part of a coordinate frame b from a coordinate frame a and a linear velocity.
        /// </summary>
        /// <param name="a">Coordinate frame a.</param>
        /// <param name="aLinear">The linear velocity.</param>
        /// <param name="aTimeStep">The time step.</param>
        /// <param name="arB">The predicted coordinate frame.</param>
        public static void GetFrameLinear(ref CoordinateFrame a, ref Vector3 aLinear, float aTimeStep, ref CoordinateFrame arB)
        {
            GetTranslation(ref a.Translation, ref aLinear, aTimeStep, out arB.Translation);
        }
        #endregion

        #region Mass properties
        public const float kC1 = (float)(1.0 / 60.0);
        public const float kC2 = (float)(1.0 / 120.0);
        public static readonly Matrix3 kCanonical = new Matrix3(kC1, kC2, kC2,
                                                                kC2, kC1, kC2,
                                                                kC2, kC2, kC1);

        public static float GetTetrahedronVolume(Vector3 w0, Vector3 w1, Vector3 w2, Vector3 w3)
        {
            const float kFactorVolume = (float)(1.0 / 6.0);

            Vector3 w1w0 = (w1 - w0);
            Vector3 w2w0 = (w2 - w0);
            Vector3 w3w0 = (w3 - w0);

            Matrix3 A = new Matrix3(w1w0.X, w2w0.X, w3w0.X,
                                    w1w0.Y, w2w0.Y, w3w0.Y,
                                    w1w0.Z, w2w0.Z, w3w0.Z);
            float detA = A.GetDeterminant();

            float volume = Math.Abs(kFactorVolume * detA);

            return volume;
        }

        /// <summary>
        /// Calculates mass properties of a tetrahedron.
        /// </summary>
        /// <remarks>
        /// From Jonathan Blow, Atman J. Binstock, July 2004, "How to find the inertia tensor
        /// (or other mass properties) of a 3D solid body represented by a triangle mash",
        /// http://number-none.com/blow/inertia/bb_inertia.doc
        /// </remarks>
        public static void GetTetrahedronProperties(Vector3 w0, Vector3 w1, Vector3 w2, Vector3 w3, float aDensity, out Matrix3 arInertiaTensor, out float arMass, out Vector3 arCenterOfMass)
        {
            const float kFactorVolume = (float)(1.0 / 6.0);
            const float kFactorCoM = (float)(1.0 / 4.0);

            Vector3 w1w0 = (w1 - w0);
            Vector3 w2w0 = (w2 - w0);
            Vector3 w3w0 = (w3 - w0);

            Matrix3 A = new Matrix3(w1w0.X, w2w0.X, w3w0.X,
                                    w1w0.Y, w2w0.Y, w3w0.Y,
                                    w1w0.Z, w2w0.Z, w3w0.Z);

            float detA = A.GetDeterminant();
            Matrix3 Cprime = detA * A * kCanonical * Matrix3.Transpose(A);

            float volume = Math.Abs(kFactorVolume * detA);
            float mass = (aDensity * volume);
            Vector3 com = (kFactorCoM * (w0 + w1 + w2 + w3));

            if (Utilities.AboutZero(mass))
            {
                arInertiaTensor = Matrix3.Zero;
                arMass = 0.0f;
                arCenterOfMass = com;
            }
            else
            {
                Matrix3 a = new Matrix3(w0.X * com.X, w0.X * com.Y, w0.X * com.Z,
                                        w0.Y * com.X, w0.Y * com.Y, w0.Y * com.Z,
                                        w0.Z * com.X, w0.Z * com.Y, w0.Z * com.Z);
                Matrix3 b = new Matrix3(com.X * w0.X, com.X * w0.Y, com.X * w0.Z,
                                        com.Y * w0.X, com.Y * w0.Y, com.Y * w0.Z,
                                        com.Z * w0.X, com.Z * w0.Y, com.Z * w0.Z);
                Matrix3 c = new Matrix3(w0.X * w0.X, w0.X * w0.Y, w0.X * w0.Z,
                                        w0.Y * w0.X, w0.Y * w0.Y, w0.Y * w0.Z,
                                        w0.Z * w0.X, w0.Z * w0.Y, w0.Z * w0.Z);

                Matrix3 Cprime2 = Cprime + (mass * (a + b + c));

                Matrix3 IT = (Matrix3.Identity * Cprime2.Trace()) - Cprime2;

                // Necessary because the math was done as columns as vectors but XNA Matrices assume rows as vectors.
                arInertiaTensor = Matrix3.Transpose(IT);
                arMass = mass;
                arCenterOfMass = com;
            }
        }
        #endregion
    }
}
