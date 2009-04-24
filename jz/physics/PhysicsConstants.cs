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

namespace jz.physics
{
    public static class PhysicsConstants
    {
        public static readonly Vector3 kDefaultGravity = 9.800722f * Vector3.Down;
        public const float kMinimumFriction = 0.0f;
        public const float kMaximumFriction = 1.0f;
        public const float kDefaultFriction = 1.0f;
        public const float kDefaultMass = 1.0f;

        /// <summary>
        /// Maximum allowed AABB in the physics system.
        /// </summary>
        /// <remarks>
        /// This is not readonly so it can be passed by reference. DO NOT MODIFY.
        /// </remarks>
        public static BoundingBox kMaximumAABB = new BoundingBox(Utilities.kMinVector3 + Vector3.One, Utilities.kMaxVector3 - Vector3.One);

        /// <summary>
        /// A tolerance value. Minimum distance in most context at which two objects are considered to be separated.
        /// </summary>
        public const float kMinimumThickness = 1e-2f;
        public const float kMinimumThickness2 = (kMinimumThickness * kMinimumThickness);

        /// <summary>
        /// The constant time step at which the physics simulation advances each tick.
        /// </summary>
        public const int kTicksPerSecond = 60;
        public const float kTimeStep = (float)(1.0 / (double)kTicksPerSecond);
        public const float kTimeStep2 = kTimeStep * kTimeStep;
        public const float kInverseTimeStep = (float)kTicksPerSecond;

        /// <summary>
        /// Damping of linear velocity. Damping is applied as linearNew = (kLinearDamping * linearOld);
        /// </summary>
        public const float kAngularDamping = 1.0f;

        /// <summary>
        /// Damping of angular velocity. Damping is applied as angularNew = (kAngularDamping * angularOld);
        /// </summary>
        public const float kLinearDamping = 1.0f;

        /// <summary>
        /// Tolerance before impulse is applied to account for penetration.
        /// </summary>
        public const float kShift = kMinimumThickness;

        /// <summary>
        /// Beta constant. Controls the step size of the solvers.
        /// </summary>
        public const float kBeta = 0.8f;

        /// <summary>
        /// Amount that the kinetic energy of an island of objects is dampened if it has increased compared
        /// to the previous tick.
        /// </summary>
        public const float kKineticEnergyFactor = 0.28f;

        /// <summary>
        /// Desired solver error.
        /// </summary>
        public const float kDesiredError = 1e-3f;

        /// <summary>
        /// Number of iterations of the solvers during each tick.
        /// </summary>
        public const int kMaxSolverIterations = 30;

        /// <summary>
        /// Threshold of the dot product of two normal vectors. Above this value, they are considered equal.
        /// </summary>
        public const float kNormalThreshold = 0.9f;

        /// <summary>
        /// Constants from Grassia, F. See PhysicsUtilities.GetFrame().
        /// </summary>
        public static readonly Radian kMinimumAngle = new Radian(1e-7f);
        public static readonly Radian kMaximumAngle = Radian.kPi;
    }
}
