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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.scene
{
    /// <summary>
    /// A scene node that encapsulates a first-person shooter style camera.
    /// </summary>
    /// <remarks>
    /// This camera controls like a first-person shooter. By default, left-clicking will activate
    /// "mouse-look" and allow you to rotate the view. The WSAD keys will move the camera forward/backward 
    /// and strafe left and right. The speed of rotation, movement, and the keys used can be configured.
    /// See the documentation for class methods for more information.
    /// </remarks>
    /// 
    /// <h2>Example</h2>
    /// <code>
    /// Cell cell = Cell.GetCell("mycell.dae");
    /// 
    /// float farPlane = 30.0f;
    /// float nearPlane = 1.0f;
    /// 
    /// // Create a new node that will be a member of the cell created above.
    /// CameraFPSNode camera = new CameraFPSNode(cell);
    /// camera.Parent = root;
    /// camera.ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 4.0f / 3.0f, nearPlane, farPlane;
    /// 
    /// // Making the camera active causes it to be the root of the world and its Cell will automatically be
    /// // posed for rendering each frame.
    /// camera.bActive = true;
    /// 
    /// // The MoveRate controls the speed of movement when keys are pressed. This scales that movement based on
    /// // the camera far and near plane depth, which is reasonable if you want to always move relative to
    /// // the size of your world.
    /// camera.MoveRate = 0.05f * (farPlane - nearPlane);
    /// 
    /// // Change the movement keys to use arrow keys (they are WSAD by default).
    /// camera.BackwardKey = Keys.Down;
    /// camera.ForwardKey = Keys.Up;
    /// camera.LeftKey = Keys.Left;
    /// camera.RightKey = Keys.Right;
    /// 
    /// // Invert the relationship between vertical mouse movement and the camera angle.
    /// camera.bFlipVertical = true;
    /// </code>
    public class CameraFPSNode : CameraNode
    {
        #region Private members
        private void _KeyHandler(KeyState aState, Keys aKey)
        {
            if (aKey == mBackward.Key) { mBackward.KeyState = aState; }
            else if (aKey == mForward.Key) { mForward.KeyState = aState; }
            else if (aKey == mLeft.Key) { mLeft.KeyState = aState; }
            else if (aKey == mRight.Key) { mRight.KeyState = aState; }
        }

        private void _MouseButtonHandler(ButtonState aState, MouseButtons aButton)
        {
            int index = (int)aButton;
            mMouseButtons[index] = aState;

            if (aButton == mMouseLookButton)
            {
                Siat siat = Siat.Singleton;

                if (aState == ButtonState.Pressed)
                {
                    MouseState state = Mouse.GetState();
                    Viewport view = siat.GraphicsDevice.Viewport;

                    if (state.X > 0 && state.Y > 0 && state.X < view.Width && state.Y < view.Height)
                    {
                        mMouseSavedX = state.X;
                        mMouseSavedY = state.Y;
                        mMouseX = mMouseSavedX;
                        mMouseY = mMouseSavedY;
                        siat.IsMouseVisible = false;
                        mbUpdateMouseLook = true;
                    }
                    else
                    {
                        mBackward.KeyState = KeyState.Up;
                        mForward.KeyState = KeyState.Up;
                        mLeft.KeyState = KeyState.Up;
                        mRight.KeyState = KeyState.Up;
                    }
                }
                else
                {
                    if (mbUpdateMouseLook)
                    {
                        Mouse.SetPosition(mMouseSavedX, mMouseSavedY);
                        mbUpdateMouseLook = false;
                        siat.IsMouseVisible = true;
                        mLastMouseChangeX = 0.0f;
                        mLastMouseChangeY = 0.0f;
                    }
                }
            }
        }

        private void _MouseMoveHandler(int aX, int aY)
        {
            if (mbUpdateMouseLook)
            {
                mMouseX = aX;
                mMouseY = aY;
            }
        }

        private bool _UpdateMove()
        {
            Siat siat = Siat.Singleton;

            return (siat.IsActive);
        }

        private bool _UpdateMouseLook()
        {
            return (mbUpdateMouseLook);
        }

        private void _UpdateMouseLookRate()
        {
            if (!mbCustomMouseLookRate)
            {
                Siat siat = Siat.Singleton;
                Viewport view = siat.GraphicsDevice.Viewport;
                double diagonal = Math.Sqrt((view.Height * view.Height) + (view.Width * view.Width));
                mMouseLookRate = (float)(2.0 / diagonal);
            }
        }
        #endregion

        #region Protected members
        protected struct KeyPair
        {
            public KeyPair(Keys aKey, KeyState aKeyState)
            {
                Key = aKey;
                KeyState = aKeyState;
            }

            public Keys Key;
            public KeyState KeyState;
        }

        protected bool mbUpdateMouseLook = false;
        protected bool mbCustomMouseLookRate = false;
        protected bool mbUpdateCamera = true;
        protected KeyPair mBackward = new KeyPair(kDefaultBackward, KeyState.Up);
        protected KeyPair mForward = new KeyPair(kDefaultForward, KeyState.Up);
        protected bool mbFlipHorizontal = false;
        protected bool mbFlipVertical = false;
        protected float mLastMouseChangeX = 0.0f;
        protected float mLastMouseChangeY = 0.0f;
        protected KeyPair mLeft = new KeyPair(kDefaultLeft, KeyState.Up);
        protected ButtonState[] mMouseButtons = new ButtonState[Enum.GetNames(typeof(MouseButtons)).Length];
        protected MouseButtons mMouseLookButton = kDefaultMouseLookButton;
        protected float mMouseLookRate = 0.0f;
        protected float mMoveRate = kDefaultMoveRate;
        protected int mMouseX = 0;
        protected int mMouseY = 0;
        protected int mMouseSavedX = 0;
        protected int mMouseSavedY = 0;
        protected float mPitch = 0.0f;
        protected KeyPair mRight = new KeyPair(kDefaultRight, KeyState.Up);
        protected float mYaw = 0.0f;
        #endregion

        #region Overrides
        protected override SceneNode SpawnClone(string aCloneId)
        {
            throw new Exception("CameraFPS cannot be cloned.");
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
                        input.AddKeyCallback(mBackward.Key, _KeyHandler);
                        input.AddKeyCallback(mForward.Key, _KeyHandler);
                        input.AddKeyCallback(mLeft.Key, _KeyHandler);
                        input.AddKeyCallback(mRight.Key, _KeyHandler);
                        input.OnMouseButton += _MouseButtonHandler;
                        input.OnMouseMove += _MouseMoveHandler;

                        _UpdateMouseLookRate();

                        siat.IsMouseVisible = true;
                    }
                    else
                    {
                        input.RemoveKeyCallback(mRight.Key, _KeyHandler);
                        input.RemoveKeyCallback(mLeft.Key, _KeyHandler);
                        input.RemoveKeyCallback(mForward.Key, _KeyHandler);
                        input.RemoveKeyCallback(mBackward.Key, _KeyHandler);
                        input.OnMouseMove -= _MouseMoveHandler;
                        input.OnMouseButton -= _MouseButtonHandler;

                        mBackward.KeyState = KeyState.Up;
                        mForward.KeyState = KeyState.Up;
                        mLastMouseChangeX = 0.0f;
                        mLastMouseChangeY = 0.0f;
                        mLeft.KeyState = KeyState.Up;
                        Array.Clear(mMouseButtons, 0, mMouseButtons.Length);
                        mRight.KeyState = KeyState.Up;
                    }
                }
            }
        }

        protected override void _OnResizeHandler()
        {
            _UpdateMouseLookRate();

            base._OnResizeHandler();
        }

        protected override void PreUpdate(Cell aCell, ref Matrix aParentRegional, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentRegional, abParentChanged);

            Vector3 vChange = Vector3.Zero;

            if (_UpdateMove())
            {
                if (mBackward.KeyState == KeyState.Down) { vChange += (Vector3.Backward); mbUpdateCamera = true; }
                if (mForward.KeyState == KeyState.Down) { vChange += (Vector3.Forward); mbUpdateCamera = true; }
                if (mLeft.KeyState == KeyState.Down) { vChange += (Vector3.Left); mbUpdateCamera = true; }
                if (mRight.KeyState == KeyState.Down) { vChange += (Vector3.Right); mbUpdateCamera = true; }

                if (mbUpdateCamera)
                {
                    if (Utilities.AboutZero(vChange.LengthSquared(), Utilities.kLooseToleranceFloat))
                    {
                        mbUpdateCamera = false;
                    }
                    else
                    {
                        vChange.Normalize();
                    }
                }
            }

            if (_UpdateMouseLook())
            {
                int mouseChangeX = (mMouseX - mMouseSavedX);
                int mouseChangeY = (mMouseY - mMouseSavedY);

                if ((mouseChangeX != 0 || mouseChangeY != 0))
                {
                    float changeX = mMouseLookRate * mouseChangeX;
                    float changeY = mMouseLookRate * mouseChangeY;

                    float lerpChangeX = MathHelper.Lerp(changeX, mLastMouseChangeX, kOldLerpFactor);
                    float lerpChangeY = MathHelper.Lerp(changeY, mLastMouseChangeY, kOldLerpFactor);

                    mYaw = (mbFlipHorizontal) ? (mYaw + lerpChangeX) : (mYaw - lerpChangeX);
                    mPitch = (mbFlipVertical) ? (mPitch + lerpChangeY) : (mPitch - lerpChangeY);

                    while (mYaw < -MathHelper.TwoPi) mYaw += MathHelper.TwoPi;
                    while (mYaw > MathHelper.TwoPi) mYaw -= MathHelper.TwoPi;
                    while (mPitch < -MathHelper.TwoPi) mPitch += MathHelper.TwoPi;
                    while (mPitch > MathHelper.TwoPi) mPitch -= MathHelper.TwoPi;

                    mPitch = Utilities.Clamp(mPitch, kMinPitch, kMaxPitch);

                    mLastMouseChangeX = lerpChangeX;
                    mLastMouseChangeY = lerpChangeY;

                    Mouse.SetPosition(mMouseSavedX, mMouseSavedY);
                    mbUpdateCamera = true;
                }
            }

            if (mbUpdateCamera)
            {
                #region Orientation
                Quaternion q = Quaternion.CreateFromAxisAngle(Vector3.Up, mYaw) * Quaternion.CreateFromAxisAngle(Vector3.Right, mPitch);
                Utilities.ToMatrix(ref q, ref mWorldWrapped.Matrix);
                #endregion

                #region Position
                Siat siat = Siat.Singleton;
                float delta = (float)siat.Time.ElapsedGameTime.TotalSeconds * mMoveRate;

                Vector3 newPosition = mWorldWrapped.Matrix.Translation + (Vector3.TransformNormal(vChange, mWorldWrapped.Matrix) * delta);
                Utilities.ToMatrix(ref newPosition, ref mWorldWrapped.Matrix);
                #endregion

                mbUpdateCamera = false;
                mFlags |= SceneNodeFlags.WorldDirty;
            }
        }
        #endregion

        public const Keys kDefaultBackward = Keys.S;
        public const Keys kDefaultForward = Keys.W;
        public const Keys kDefaultLeft = Keys.A;
        public const MouseButtons kDefaultMouseLookButton = MouseButtons.Left;
        public const float kDefaultMoveRate = (float)(1.0 / 512.0);
        public const Keys kDefaultRight = Keys.D;
        public const float kOldLerpFactor = 0.5f;
        public const float kMinPitch = -MathHelper.PiOver2 * (float)(2.0 / 3.0);
        public const float kMaxPitch = -kMinPitch;

        public CameraFPSNode(Cell aCell) : base(aCell) { }
        public CameraFPSNode(Cell aCell, string aId) : base(aCell, aId) { }

        /// <summary>
        /// Gets/sets the rate of rotation of the camera when the 
        /// mouse is moved with the MouseLookButton depressed.
        /// </summary>
        /// <remarks>
        /// An example value for an 800 x 600 display is: 0.002
        /// </remarks>
        public float MouseLookRate { get { return mMouseLookRate; } set { mMouseLookRate = value; mbCustomMouseLookRate = true;  } }

        /// <summary>
        /// Gets/sets the key that moves the camera backward.
        /// </summary>
        public Keys BackwardKey { get { return mBackward.Key; } set { mBackward.Key = value; } }

        /// <summary>
        /// Gets/sets the key that moves the camera forward.
        /// </summary>
        public Keys ForwardKey { get { return mForward.Key; } set { mForward.Key = value; } }

        /// <summary>
        /// Inverts the relationship between horizontal mouse movement and the camera.
        /// </summary>
        public bool bFlipHorizontal { get { return mbFlipHorizontal; } set { mbFlipHorizontal = value; } }

        /// <summary>
        /// Inverts the relationship between vertical mouse movement and the camera.
        /// </summary>
        public bool bFlipVertical { get { return mbFlipVertical; } set { mbFlipVertical = value; } }

        /// <summary>
        /// Gets/sets the key that moves the camera left.
        /// </summary>
        public Keys LeftKey { get { return mLeft.Key; } set { mLeft.Key = value; } }

        /// <summary>
        /// Gets/sets the rate of movement when a key is depressed.
        /// </summary>
        /// <remarks>
        /// An example value for a world that is 26.0 units deep is: 1.3
        /// </remarks>
        public float MoveRate { get { return mMoveRate; } set { mMoveRate = value; } }

        /// <summary>
        /// Gets/sets the key that moves the camera right.
        /// </summary>
        public Keys RightKey { get { return mRight.Key; } set { mRight.Key = value; } }

        /// <summary>
        /// Gets/sets the button used to angle the camera with the mouse.
        /// </summary>
        public MouseButtons MouseLookButton
        {
            get
            {
                return mMouseLookButton;
            }
            
            set
            {
                if (value != mMouseLookButton)
                {
                    _MouseButtonHandler(ButtonState.Released, mMouseLookButton);

                    mMouseLookButton = value;

                    _MouseButtonHandler(mMouseButtons[(int)mMouseLookButton], mMouseLookButton);
                }
            }
        }
    }
}
