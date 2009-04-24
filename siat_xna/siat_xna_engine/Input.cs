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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace siat
{
   
    /// <summary>
    /// Indicates a specific mouse button for a mouse button callback.
    /// </summary>
    public enum MouseButtons
    {
        Left,
        Middle,
        Right,
        XButton1,
        XButton2
    }

    public delegate void KeyEventCallback(KeyState aState, Keys aKey);
    public delegate void MouseButtonEventCallback(ButtonState aState, MouseButtons aButton);
    public delegate void MouseMoveEventCallback(int aRelativeX, int aRelativeY);
    public delegate void MouseMoveDeltaEventCallback(int aDeltaX, int aDeltaY);
    public delegate void MouseWheelEventCallback(int aAbsoluteValue);
    public delegate void MouseWheelDeltaEventCallback(int aDeltaValue);
    
    /// <summary>
    /// Basic Input manager. Handles collection and distribution of mouse and keyboard events.
    /// </summary>
    /// <remarks>
    /// An Input object is automatically instantiated and update by Siat.
    /// </remarks>
    /// 
    /// \sa siat.Siat
    public sealed class Input
    {
        #region Singleton implementations
        private static readonly Input mSingleton = new Input();

        public static Input Singleton
        {
            get
            {
                return mSingleton;
            }
        }
        #endregion

        #region Private members
        private bool mbKeyboardEnabled = false;
        private KeyboardState mPreviousKeyboardState;
        private bool mbMouseEnabled = false;
        private MouseState mPreviousMouseState;
        private Dictionary<Keys, KeyEventCallback> mKeyCallbacks = new Dictionary<Keys, KeyEventCallback>();

        private Input()
        { }
        #endregion

        public void Initialize()
        {
            KeyboardEnabled = true;
            MouseEnabled = true;
        }

        public bool KeyboardEnabled
        {
            get
            {
                return mbKeyboardEnabled;
            }
            set
            {
                mbKeyboardEnabled = value;
                if (mbKeyboardEnabled)
                {
                    mPreviousKeyboardState = Keyboard.GetState();
                }
            }
        }
        
        public bool MouseEnabled
        {
            get
            {
                return mbMouseEnabled;
            }
            set
            {
                mbMouseEnabled = value;
                if (mbMouseEnabled)
                {
                    mPreviousMouseState = Mouse.GetState();
                }
            }
        }

        public void AddKeyCallback(Keys aKey, KeyEventCallback aCallback)
        {
            if (mKeyCallbacks.ContainsKey(aKey))
            {
                mKeyCallbacks[aKey] += aCallback;
            }
            else
            {
                mKeyCallbacks.Add(aKey, aCallback);
            }
        }
        
        public void RemoveKeyCallback(Keys aKey, KeyEventCallback aCallback)
        {
            mKeyCallbacks[aKey] -= aCallback;
            
            if (mKeyCallbacks[aKey] == null)
            {
                mKeyCallbacks.Remove(aKey);
            }
        }
        
        public event MouseButtonEventCallback     OnMouseButton;
        public event MouseMoveEventCallback       OnMouseMove;
        public event MouseMoveDeltaEventCallback  OnMouseMoveDelta;
        public event MouseWheelEventCallback      OnMouseWheel;
        public event MouseWheelDeltaEventCallback OnMouseWheelDelta;
        
        public void Update()
        {
            if (mbMouseEnabled)
            {
                MouseState mouseState = Mouse.GetState();

                if (OnMouseButton != null)
                {
                    if (mouseState.LeftButton != mPreviousMouseState.LeftButton)
                    {
                        OnMouseButton(mouseState.LeftButton, MouseButtons.Left);
                    }
                    if (mouseState.MiddleButton != mPreviousMouseState.MiddleButton)
                    {
                        OnMouseButton(mouseState.MiddleButton, MouseButtons.Middle);
                    }
                    if (mouseState.RightButton != mPreviousMouseState.RightButton)
                    {
                        OnMouseButton(mouseState.RightButton, MouseButtons.Right);
                    }
                    if (mouseState.XButton1 != mPreviousMouseState.XButton1)
                    {
                        OnMouseButton(mouseState.XButton1, MouseButtons.XButton1);
                    }
                    if (mouseState.XButton2 != mPreviousMouseState.XButton2)
                    {
                        OnMouseButton(mouseState.XButton2, MouseButtons.XButton2);
                    }                       
                }             
                
                if (mouseState.X != mPreviousMouseState.X || mouseState.Y != mPreviousMouseState.Y)
                {
                    if (OnMouseMove != null)
                    {
                        OnMouseMove(mouseState.X, mouseState.Y);
                    }
                    if (OnMouseMoveDelta != null)
                    {
                        OnMouseMoveDelta(mouseState.X - mPreviousMouseState.X, mouseState.Y - mPreviousMouseState.Y);
                    }
                }                       
                
                if (mouseState.ScrollWheelValue != mPreviousMouseState.ScrollWheelValue)
                {
                    if (OnMouseWheel != null)
                    {
                        OnMouseWheel(mouseState.ScrollWheelValue);
                    }
                    if (OnMouseWheelDelta != null)
                    {
                        OnMouseWheelDelta(mouseState.ScrollWheelValue - mPreviousMouseState.ScrollWheelValue);
                    }
                }
                
                mPreviousMouseState = mouseState;
            }
           
            if (mbKeyboardEnabled)
            {
                KeyboardState keyboardState = Keyboard.GetState();
                
                foreach (KeyValuePair<Keys, KeyEventCallback> e in mKeyCallbacks)
                {
                    if (keyboardState.IsKeyDown(e.Key) && !mPreviousKeyboardState.IsKeyDown(e.Key))
                    {
                        e.Value(KeyState.Down, e.Key);
                    }
                    else if (keyboardState.IsKeyUp(e.Key) && !mPreviousKeyboardState.IsKeyUp(e.Key))
                    {
                        e.Value(KeyState.Up, e.Key);
                    }
                }
                
                mPreviousKeyboardState = keyboardState;
            }
        }
    }
}
