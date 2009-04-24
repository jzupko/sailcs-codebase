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
using siat.render;

using jz;
using jz.physics;
using jz.physics.narrowphase;

namespace siat.scene
{
    public class PhysicsSceneNode : SceneNode
    {
        #region Protected members
        protected World mWorld = new World();
        #endregion

        #region Overrides
        protected override void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            mWorld.Tick((float)Siat.Singleton.Time.ElapsedGameTime.TotalSeconds);

            base.PreUpdate(aCell, ref aParentWorld, abParentChanged);
        }

        protected override SceneNode SpawnClone(string aCloneId)
        {
            throw new Exception(this.ToString() + " cannot be cloned.");
        }
        #endregion

        public PhysicsSceneNode(WorldTree aTree)
            : base()
        {
            mFlags |= SceneNodeFlags.ExcludeFromBounding | SceneNodeFlags.ExcludeFromShadowing;

            WorldBody world = new WorldBody(aTree);
            world.World = mWorld;
        }

        public PhysicsSceneNode(string aId, WorldTree aTree)
            : base(aId)
        {
            mFlags |= SceneNodeFlags.ExcludeFromBounding | SceneNodeFlags.ExcludeFromShadowing;

            WorldBody world = new WorldBody(aTree);
            world.World = mWorld;
        }

        public World World { get { return mWorld; } }
    }
}
