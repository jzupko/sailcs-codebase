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

namespace siat.scene
{
    /// <summary>
    /// A scene node that encapsulates an instance of a camera.
    /// </summary>
    public class CameraNode : SceneNode
    {
        #region Protected members
        protected bool mbActive = false;
        protected Cell mCell = null;
        protected bool mbProjectionDirty = false;
        protected bool mbViewDirty = false;
        protected Matrix mProjection = Matrix.Identity;
        protected Matrix mView = Matrix.Identity;

        protected virtual void _OnResizeHandler()
        {
            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            float near;
            float far;
            Utilities.ExtractNearFar(ref mProjection, out near, out far);

            int width = gd.PresentationParameters.BackBufferWidth;
            int height = gd.PresentationParameters.BackBufferHeight;

            float aspectRatio = (float)width / (float)height;
            float fov = Utilities.ExtractFov(ref mProjection);

            ProjectionTransform = Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, near, far);
        }
        #endregion

        #region Overrides
        protected override void  PopulateClone(SceneNode aNode)
        {
            base.PopulateClone(aNode);

            CameraNode camera = (CameraNode)aNode;
            camera.ProjectionTransform = mProjection;
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new CameraNode(mCell, aCloneId);
        }

        protected override void PostUpdate(Cell aCell, bool abChanged)
        {
            if (abChanged)
            {
                Matrix.Invert(ref mWorldWrapped.Matrix, out mView);
                mbViewDirty = true;
            }

            if (mbActive && mbViewDirty)
            {
                Shared.ViewTransform = mView;
                mbViewDirty = false;
            }

            if (mbActive && mbProjectionDirty)
            {
                Shared.ProjectionTransform = mProjection;
                mbProjectionDirty = false;
            }

            base.PostUpdate(aCell, abChanged);
        }
        #endregion

        public const float kUpdateSphereFactor = 2.0f;

        public CameraNode(Cell aCell) : base() { mCell = aCell; }
        public CameraNode(Cell aCell, string aId) : base(aId) { mCell = aCell; }

        public Cell Cell { get { return mCell; } set { mCell = value; } }
        public Matrix ProjectionTransform { get { return mProjection; } set { mProjection = value; mbProjectionDirty = true; } }
        public Matrix ViewTransform { get { return mView; } }

        public void StartPose()
        {
            if (mCell != null) mCell.FrustumPose(null);
        }

        public void StartUpdate()
        {
            if (mCell != null)
            {
                float near, far;
                Utilities.ExtractNearFar(ref mProjection, out near, out far);
                Update(null, ref Utilities.kIdentity, false);
                mCell.Update(ref Utilities.kIdentity);
            }
        }

        public virtual bool bActive
        {
            get
            {
                return mbActive;
            }

            set
            {
                if (value != mbActive)
                {
                    mbActive = value;

                    Siat siat = Siat.Singleton;
                    if (siat.ActiveCamera != this)
                    {
                        siat.ActiveCamera = this;
                    }

                    if (mbActive)
                    {
                        mbProjectionDirty = true;
                        mbViewDirty = true;

                        siat.OnResize += _OnResizeHandler;
                    }
                    else
                    {
                        siat.OnResize -= _OnResizeHandler;
                    }
                }
            }
        }
    }
}
