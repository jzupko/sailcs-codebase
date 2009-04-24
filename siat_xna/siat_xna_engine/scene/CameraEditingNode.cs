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
using Microsoft.Xna.Framework.Input;
using System;

namespace siat.scene
{
    /// <summary>
    /// A scene node that encapsulates a Maya style editor camera.
    /// </summary>
    public class CameraEditingNode : CameraNode
    {
        #region Protected members
        protected float mChangeTolerance = 0.0f;
        protected Vector3 mCurrentTarget = Vector3.Zero;
        protected bool mbDistanceScaleDirty = true;
        protected Vector3 mNextTarget = Vector3.Zero;
        protected float mDistance = 0.0f;
        protected float mDistanceScale = 0.0f;
        protected float mLastMouseChangeX = 0.0f;
        protected float mLastMouseChangeY = 0.0f;
        protected float mMaxDistance = float.MaxValue;
        protected ButtonState[] mMouseButtons = new ButtonState[Enum.GetNames(typeof(MouseButtons)).Length];
        protected int mMouseChangeX = 0;
        protected int mMouseChangeY = 0;
        protected float mMinPitch = -MathHelper.ToRadians(89.0f);
        protected float mMaxPitch = MathHelper.ToRadians(89.0f);
        protected float mPitch = 0.0f;
        protected bool mbPriorMouseCursorSetting = false;
        protected bool mbUpdateCamera = true;
        protected int mWheelChange = 0;
        protected float mWheelDesiredChange = 0.0f;
        protected float mYaw = 0.0f;

        protected void _MouseButtonHandler(ButtonState aState, MouseButtons aButtons)
        {
            int index = (int)aButtons;
            mMouseButtons[index] = aState;
        }

        protected void _MouseMoveDeltaHandler(int aDeltaX, int aDeltaY)
        {
            if (_UpdateMove())
            {
                mMouseChangeX += aDeltaX;
                mMouseChangeY += aDeltaY;
            }
            else if (_UpdateZoom())
            {
                mWheelChange += (int)(aDeltaY * kZoomButtonFactor);
            }
        }

        protected void _MouseWheelHandler(int aDeltaValue)
        {
            mWheelChange += aDeltaValue;
        }

        protected bool _UpdateMove()
        {
            Siat siat = Siat.Singleton;

            return mMouseButtons[kRotateButton] == ButtonState.Pressed && siat.IsActive;
        }

        protected bool _UpdateZoom()
        {
            Siat siat = Siat.Singleton;

            return (mMouseButtons[kZoomButton] == ButtonState.Pressed && siat.IsActive);
        }

        protected void _UpdateDistance()
        {
            if (mbDistanceScaleDirty)
            {
                float near, far;
                Utilities.ExtractNearFar(ref mProjection, out near, out far);
                float eighthFar = 0.125f * far;
                float sixteenthFar = 0.5f * eighthFar;

                mDistanceScale = sixteenthFar;
                mDistance = eighthFar;
                mChangeTolerance = mDistanceScale * kChangeToleranceFactor;
                mbDistanceScaleDirty = false;
            }
        }
        #endregion

        #region Overrides
        protected override SceneNode SpawnClone(string aCloneId)
        {
            throw new Exception("CameraEditingNode cannot be cloned.");
        }

        public override bool bActive
        {
            get
            {
                return base.bActive;
            }
            set
            {
                if (value != mbActive)
                {
                    base.bActive = value;

                    Siat siat = Siat.Singleton;
                    Input input = Input.Singleton;

                    if (mbActive)
                    {
                        input.OnMouseButton += _MouseButtonHandler;
                        input.OnMouseWheelDelta += _MouseWheelHandler;
                        input.OnMouseMoveDelta += _MouseMoveDeltaHandler;

                        mbPriorMouseCursorSetting = siat.IsMouseVisible;
                        siat.IsMouseVisible = true;
                    }
                    else
                    {
                        siat.IsMouseVisible = mbPriorMouseCursorSetting;

                        input.OnMouseMoveDelta -= _MouseMoveDeltaHandler;
                        input.OnMouseWheelDelta -= _MouseWheelHandler;
                        input.OnMouseButton -= _MouseButtonHandler;

                        mMouseChangeX = 0;
                        mMouseChangeY = 0;
                        mLastMouseChangeX = 0.0f;
                        mLastMouseChangeY = 0.0f;
                        mWheelChange = 0;
                        mWheelDesiredChange = 0.0f;
                    }
                }
            }
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);

            Siat siat = Siat.Singleton;
            float delta = (float)siat.Time.ElapsedGameTime.TotalSeconds;

            if (mbProjectionDirty)
            {
                _UpdateDistance();
            }

            if ((mFlags & SceneNodeFlags.LocalDirty) != 0)
            {
                Matrix newTransform = mLocal * aParentWorld;
                Vector3 diff = newTransform.Translation - mWorldWrapped.Matrix.Translation;
                mCurrentTarget = mCurrentTarget + diff;
                mNextTarget = mNextTarget + diff;
                mFlags &= ~SceneNodeFlags.LocalDirty;
            }

            if (mWheelChange != 0)
            {
                mWheelDesiredChange += kChangeRate * mDistanceScale * (mWheelChange);
                mWheelChange = 0;
            }

            if (!Utilities.AboutEqual(ref mCurrentTarget, ref mNextTarget))
            {
                Vector3 change = kChangeVelocity * delta * (mNextTarget - mCurrentTarget);

                mCurrentTarget += change;

                mbUpdateCamera = true;
            }

            if (!Utilities.AboutZero(mWheelDesiredChange, mChangeTolerance))
            {
                float change = kChangeVelocity * delta * mWheelDesiredChange;

                mDistance += change;

                if (Utilities.LessThan(mDistance, 0.0f))
                {
                    mDistance = 0.0f;
                    mWheelDesiredChange = 0.0f;
                }
                else if (Utilities.GreaterThan(mDistance, mMaxDistance))
                {
                    mDistance = mMaxDistance;
                    mWheelDesiredChange = 0.0f;
                }
                else
                {
                    mWheelDesiredChange -= change;
                }

                mbUpdateCamera = true;
            }

            if ((mMouseChangeX != 0 || mMouseChangeY != 0) && _UpdateMove())
            {
                float changeX = kMouseMoveChangeRate * mMouseChangeX;
                float changeY = kMouseMoveChangeRate * mMouseChangeY;
                mMouseChangeX = 0;
                mMouseChangeY = 0;

                float lerpChangeX = MathHelper.Lerp(changeX, mLastMouseChangeX, kOldLerpFactor);
                float lerpChangeY = MathHelper.Lerp(changeY, mLastMouseChangeY, kOldLerpFactor);

                mYaw += lerpChangeX;
                mPitch += lerpChangeY;

                mPitch = Utilities.Clamp(mPitch, mMinPitch, mMaxPitch);

                mLastMouseChangeX = lerpChangeX;
                mLastMouseChangeY = lerpChangeY;

                mbUpdateCamera = true;
            }

            if (mbUpdateCamera)
            {
                #region Orientation
                Quaternion q = Quaternion.CreateFromAxisAngle(Vector3.Up, mYaw) * Quaternion.CreateFromAxisAngle(Vector3.Right, mPitch);
                Utilities.ToMatrix(ref q, ref mWorldWrapped.Matrix);
                #endregion

                #region Position
                Vector3 vChange = Vector3.TransformNormal(Vector3.Backward, mWorldWrapped.Matrix) * mDistance;
                Vector3 newPosition = mCurrentTarget + vChange;
                Utilities.ToMatrix(ref newPosition, ref mWorldWrapped.Matrix);
                #endregion

                mFlags |= SceneNodeFlags.WorldDirty;
            }
        }
        #endregion

        public const float kChangeRate = (float)(1.0 / 600.0);
        public const float kChangeToleranceFactor = 0.1f;
        public const float kChangeVelocity = 2.0f;
        public const float kMouseMoveChangeRate = (float)(1.0 / 512.0);
        public const float kZoomButtonFactor = 1.5f;
        public const float kOldLerpFactor = 0.5f;
        public const int kRotateButton = (int)MouseButtons.Left;
        public const int kZoomButton = (int)MouseButtons.Right;

        public CameraEditingNode(Cell aCell) : base(aCell) { }
        public CameraEditingNode(Cell aCell, string aId) : base(aCell, aId) { }

        /// <summary>
        /// The distance scale determines how fast the camera zooms on a mouse wheel scroll.
        /// </summary>
        /// <remarks>
        /// The distance scale is automatically calculated based on the containing cell of this
        /// camera. Calling this function forces the CameraEditingNode object to recalculate the
        /// scale in situations where it wouldn't be detected normally.
        /// </remarks>
        public void ResetDistanceScale() { mbDistanceScaleDirty = true; }

        public float Distance { get { return mDistance; } set { mDistance = value; } }
        public float MaxDistance { get { return mMaxDistance; } set { mMaxDistance = value; } }
        public float Pitch { get { return mPitch; } set { mPitch = value; } }
        public float Yaw { get { return mYaw; } set { mYaw = value; } }

        public Vector3 ForceTarget { set { mCurrentTarget = value; mNextTarget = value; } }

        /// <summary>
        /// The target that this camera looks at.
        /// </summary>
        public Vector3 Target { get { return mCurrentTarget; } set { mNextTarget = value; } }
    }
}
