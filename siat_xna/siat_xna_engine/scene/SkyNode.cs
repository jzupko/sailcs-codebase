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
using siat.render;

namespace siat.scene
{
    public class SkyNode : MeshPartNode
    {
        #region Overrides
        public override bool bMyShadowRequiresUpdate { get { return false; } }

        public override void FrustumPose(IPoseable aPoseable)
        {
            RenderRoot.PoseOperations.Sky(mWorldWrapped, mMeshPart, mMaterial, mEffect);
        }

        public override bool LightingPose(LightNode aLight)
        {
            return false;
        }

        public override void Pick(Cell aCell, ref Ray aWorldRay)
        {}

        protected override void PreUpdate(Cell aCell, ref Microsoft.Xna.Framework.Matrix aParentWorld, bool abParentChanged)
        {
            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);

            if ((mFlags & SceneNodeFlags.LocalDirty) != 0)
            {
                LocalPosition = Vector3.Transform(Shared.InverseViewTransform.Translation, Matrix.Invert(WorldTransform));
            }
            else
            {
                // Force the sky to always be centered around the camera (gives the illusion of the sky being
                // so far away that it never "moves", only rotates).
                WorldPosition = Shared.InverseViewTransform.Translation;
            }
        }

        public override void ShadowingPose(LightNode aLight)
        { }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            return new SkyNode(aCloneId);
        }
        #endregion

        public SkyNode() : base() { mFlags |= SceneNodeFlags.ExcludeFromBounding; }
        public SkyNode(string aId) : base(aId) { mFlags |= SceneNodeFlags.ExcludeFromBounding; }
    }
}
