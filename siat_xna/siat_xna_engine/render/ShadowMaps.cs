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
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.render
{
    public static class ShadowMaps
    {
        public const int kShadowMapDimension = 512;
        public const float kInverseShadowMapDimension = (float)(1.0 / kShadowMapDimension);                
        public const int kCount = 5;

        public static readonly Matrix kShadowTransformPost 
            = new Matrix(0.5f,  0,    0, 0,
                         0,    -0.5f, 0, 0,
                         0,     0,    1, 0,
                         0.5f + 0.5f * kInverseShadowMapDimension, 0.5f + 0.5f * kInverseShadowMapDimension, 0, 1);

        #region Private members
        private static bool msbLoaded = false;
        private static DepthStencilBuffer msDepthStencilBuffer = null;
        private static RenderRoot.RenderTargetPackage[] msTargets = new RenderRoot.RenderTargetPackage[kCount];
        private static List<int> msFreeList = new List<int>(kCount);
        #endregion

        static ShadowMaps()
        {
            for (int i = 0; i < kCount; i++) { msFreeList.Add(i); }
        }

        public static void OnLoad()
        {
            if (!msbLoaded)
            {
                Siat siat = Siat.Singleton;
                msDepthStencilBuffer = new DepthStencilBuffer(siat.GraphicsDevice,
                    kShadowMapDimension, kShadowMapDimension, DepthFormat.Depth24Stencil8);

                for (int i = 0; i < kCount; i++)
                {
                    msTargets[i] = new RenderRoot.RenderTargetPackage(0,
                        new RenderTarget2D(siat.GraphicsDevice,
                        kShadowMapDimension, kShadowMapDimension, 1, SurfaceFormat.Single,
                        RenderTargetUsage.PlatformContents), msDepthStencilBuffer, Color.White);
                }

                msbLoaded = true;
            }
        }

        public static void OnUnload()
        {
            if (msbLoaded)
            {
                for (int i = kCount - 1; i >= 0; i--) { msTargets[i].Dispose(); msTargets[i] = null; }
                msDepthStencilBuffer.Dispose(); msDepthStencilBuffer = null;
                msbLoaded = false;
            }
        }

        public static int Grab()
        {
            if (msFreeList.Count == 0)
            {
                return -1;
            }
            else
            {
                int index = msFreeList[msFreeList.Count - 1];
                msFreeList.RemoveAt(msFreeList.Count - 1);

                return index;
            }
        }

        public static RenderRoot.RenderTargetPackage Get(int i) { return msTargets[i]; }

        public static void Release(int i)
        {
            msFreeList.Add(i);
        }
    }
}
