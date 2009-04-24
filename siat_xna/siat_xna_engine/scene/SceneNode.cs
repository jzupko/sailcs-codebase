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

namespace siat.scene
{
    [Flags]
    public enum SceneNodeFlags
    {
        LocalDirty = (1 << 0),
        WorldDirty = (1 << 1),
        ExcludeFromBounding = (1 << 2),
        PoseableDirty = (1 << 3),
        ExcludeFromShadowing = (1 << 4),
        IgnoreParent = (1 << 5)
    }

    /// <summary>
    /// Base class for all nodes in a scene graph.
    /// </summary>
    /// <remarks>
    /// Scene graph nodes are spatial instances. Usually they are instances of renderable objects 
    /// (such as siat.scene.MeshPartNode) but they can be anything with a spatial transform.
    /// 
    /// Scene graph nodes usually "instance" shared objects. For example, a siat.scene.MeshPartNode
    /// is a specific instance of a siat.render.MeshPart. The shared data of siat.render.MeshPart
    /// (vertices, indices) have explicit concrete instances at any given time through a
    /// siat.scene.MeshPartNode. This allows many copies of the same thing at different locations 
    /// to exist in the world at the same time at low performance overhead.
    /// </remarks>
    /// 
    /// \sa siat.scene.MeshPartNode
    public class SceneNode : TreeNode<SceneNode>
    {
        #region Private members
        private static WeakRefContainer<string, SceneNode> msGlobalSceneNodeCollection = new WeakRefContainer<string, SceneNode>(string.Empty);
        private string mId = string.Empty;

        private bool mbDirty = false;

        private void _CloneChildren(SceneNode aToParent, string aCloneIdPostfix)
        {
            for (SceneNode e = mFirstChild; e != null; e = e.mNextSibling)
            {
                e.Clone(aToParent, aCloneIdPostfix);
            }
        }

        private void _UpdateITWorld()
        {
            mITWorldWrapped.Matrix = Matrix3.CreateFromUpperLeft(ref mWorldWrapped.Matrix);
            Matrix3.Invert(ref mITWorldWrapped.Matrix, out mITWorldWrapped.Matrix);
            mITWorldWrapped.Matrix.Transpose();
        }

        private bool _Update(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            bool bChanged = false;

            if ((mFlags & SceneNodeFlags.IgnoreParent) == 0)
            {
                if ((mFlags & SceneNodeFlags.WorldDirty) != 0)
                {
                    _UpdateITWorld();
                    Matrix invParent = Matrix.Invert(aParentWorld);
                    mLocal = mWorldWrapped.Matrix * invParent;

                    mFlags &= ~SceneNodeFlags.WorldDirty;
                    mFlags &= ~SceneNodeFlags.LocalDirty;
                    bChanged = true;
                }
                else if (abParentChanged || (mFlags & SceneNodeFlags.LocalDirty) != 0)
                {
                    mWorldWrapped.Matrix = mLocal * aParentWorld;
                    _UpdateITWorld();

                    mFlags &= ~SceneNodeFlags.LocalDirty;
                    bChanged = true;
                }
            }
            else
            {
                if ((mFlags & SceneNodeFlags.WorldDirty) != 0)
                {
                    _UpdateITWorld();
                    mLocal = mWorldWrapped.Matrix;

                    mFlags &= ~SceneNodeFlags.WorldDirty;
                    mFlags &= ~SceneNodeFlags.LocalDirty;
                    bChanged = true;
                }
                else if ((mFlags & SceneNodeFlags.LocalDirty) != 0)
                {
                    mWorldWrapped.Matrix = mLocal;
                    _UpdateITWorld();

                    mFlags &= ~SceneNodeFlags.LocalDirty;
                    bChanged = true;
                }
            }

            bool bBoundingUpdate = bChanged;
            for (SceneNode e = mFirstChild; e != null; e = e.mNextSibling)
            {
                bBoundingUpdate = e.Update(aCell, ref mWorldWrapped.Matrix, bChanged) || bBoundingUpdate;
            }

            mbDirty = bBoundingUpdate;

            return bBoundingUpdate;
        }

        private void _UpdateBounding()
        {
            mbValidBounding = false;
            mWorldBounding = default(BoundingSphere);
            SceneNode e = mFirstChild;

            for (; e != null; e = e.mNextSibling)
            {
                if (e.mbValidBounding && (e.Flags & SceneNodeFlags.ExcludeFromBounding) == 0)
                {
                    mWorldBounding = e.mWorldBounding;
                    mbValidBounding = true;
                    e = e.mNextSibling;
                    break;
                }
            }

            for (; e != null; e = e.mNextSibling)
            {
                if (e.mbValidBounding && (e.Flags & SceneNodeFlags.ExcludeFromBounding) == 0)
                {
                    BoundingSphere.CreateMerged(ref mWorldBounding, ref e.mWorldBounding, out mWorldBounding);
                }
            }
        }
        #endregion

        #region Protected members
        protected SceneNodeFlags mFlags = SceneNodeFlags.LocalDirty;
        protected Matrix mLocal = Matrix.Identity;
        protected MatrixWrapper mWorldWrapped = new MatrixWrapper();
        protected Matrix3Wrapper mITWorldWrapped = new Matrix3Wrapper();
        protected bool mbValidBounding = false;
        protected BoundingSphere mWorldBounding = new BoundingSphere();
        #endregion

        /// <summary>
        /// Called to assign the members of a clone of this node.
        /// </summary>
        /// <param name="aNode">The clone of this node.</param>
        protected virtual void PopulateClone(SceneNode aNode)
        {
            aNode.mLocal = mLocal;
        }

        /// <summary>
        /// Called to create an instance of this node's type for cloning.
        /// </summary>
        /// <param name="aCloneId">The string id for the clone.</param>
        /// <returns>A scene node of this type.</returns>
        protected virtual SceneNode SpawnClone(string aCloneId)
        {
            return new SceneNode(aCloneId);
        }

        /// <summary>
        /// Called before the matrix transforms of this node are updated.
        /// </summary>
        /// <param name="aCell">The cell that this node belongs to for this update pass.</param>
        /// <param name="aParentWorld">The world transform of this node's parent.</param>
        /// <param name="abParentChanged">Did the parent change during this update pass?</param>
        protected virtual void PreUpdate(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        { }

        /// <summary>
        /// Called after the matrix transforms of this node are updated.
        /// </summary>
        /// <param name="aCell">The cell that this node belongs to for this update pass.</param>
        /// <param name="abChanged">Did this node change during this update pass?</param>
        protected virtual void PostUpdate(Cell aCell, bool abChanged)
        { }

        public const string kDefaultCloneIdPostfix = "_clone";

        public SceneNode() {}
        public SceneNode(string aId)
        {
            mId = aId;

            msGlobalSceneNodeCollection.Add(mId, this);
        }

        ~SceneNode()
        {
            msGlobalSceneNodeCollection.Remove(mId);
        }

        public bool bDirty { get { return mbDirty; } }
        public bool bValidBounding { get { return mbValidBounding; } }
        public SceneNodeFlags Flags { get { return mFlags; } }
        public Matrix3 ITWorldTransform { get { return mITWorldWrapped.Matrix; } }
        public BoundingSphere WorldBounding { get { return mWorldBounding; } }
        
        public SceneNode Clone(SceneNode aToParent) { return Clone(aToParent, kDefaultCloneIdPostfix); }
        public SceneNode Clone(SceneNode aToParent, string aCloneIdPostfix)
        {
            SceneNode clone = SpawnClone(mId + aCloneIdPostfix);
            PopulateClone(clone);

            _CloneChildren(clone, aCloneIdPostfix);
            clone.Parent = aToParent;

            return clone;
        }

        public static bool Exists(string aId)
        {
            return (msGlobalSceneNodeCollection.Retrieve(aId) != null);
        }

        /// <summary>
        /// Returns the node with the given id or null if a node with the id does not exist.
        /// </summary>
        /// <typeparam name="T">The type to cast the return to.</typeparam>
        /// <param name="aId">The id of the node to find.</param>
        /// <returns>The node or null if the id was not found.</returns>
        public static T Find<T>(string aId) where T : SceneNode
        {
            SceneNode ret = msGlobalSceneNodeCollection.Retrieve(aId);

            return (ret != null) ? (T)ret : null;
        }

        /// <summary>
        /// Like SceneNode.Find() but queues the node for retrieval when it exists.
        /// </summary>
        /// <param name="aId">The id of the node to retrieve.</param>
        /// <param name="aAction">The delegate to execute when the node exists.</param>
        public static void Retrieve(string aId, Action<SceneNode> aAction)
        {
            msGlobalSceneNodeCollection.QueueForRetrieval(aId, aAction);
        }

        public static void TickRetrieveQueue()
        {
            msGlobalSceneNodeCollection.Tick();
        }

        public string Id
        {
            get
            {
                return mId;
            }

            set
            {
                string oldId = mId;
                mId = value;

                msGlobalSceneNodeCollection.Update(mId, this, oldId);
            }
        }

        public Quaternion LocalOrientation
        {
            get
            {
                return Quaternion.CreateFromRotationMatrix(mLocal);
            }

            set
            {
                Utilities.ToMatrix(ref value, ref mLocal);
                mFlags |= SceneNodeFlags.LocalDirty;
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                return mLocal.Translation;
            }

            set
            {
                Utilities.ToMatrix(ref value, ref mLocal);
                mFlags |= SceneNodeFlags.LocalDirty;
            }
        }

        public Matrix LocalTransform
        {
            get
            {
                return mLocal;
            }

            set
            {
                mLocal = value;
                mFlags |= SceneNodeFlags.LocalDirty;
            }
        }

        public override SceneNode Parent
        {
            get
            {
                return base.Parent;
            }
            set
            {
                base.Parent = value;
                mFlags |= SceneNodeFlags.LocalDirty;
            }
        }

        public Quaternion WorldOrientation
        {
            get
            {
                return Quaternion.CreateFromRotationMatrix(mWorldWrapped.Matrix);
            }

            set
            {
                Utilities.ToMatrix(ref value, ref mWorldWrapped.Matrix);
                mFlags |= SceneNodeFlags.WorldDirty;
            }
        }

        public Vector3 WorldPosition
        {
            get
            {
                return mWorldWrapped.Matrix.Translation;
            }

            set
            {
                Utilities.ToMatrix(ref value, ref mWorldWrapped.Matrix);
                mFlags |= SceneNodeFlags.WorldDirty;
            }
        }

        public Matrix WorldTransform
        {
            get
            {
                return mWorldWrapped.Matrix;
            }

            set
            {
                mWorldWrapped.Matrix = value;
                mFlags |= SceneNodeFlags.WorldDirty;
            }
        }

        public bool Update(Cell aCell, ref Matrix aParentWorld, bool abParentChanged)
        {
            if (OnUpdateBegin != null) OnUpdateBegin(aCell, this);
            PreUpdate(aCell, ref aParentWorld, abParentChanged);
            bool bReturn = _Update(aCell, ref aParentWorld, abParentChanged);
                if (bReturn) _UpdateBounding();
            PostUpdate(aCell, bReturn);
            if (bReturn && OnUpdateEnd != null) OnUpdateEnd(aCell, this);

            return bReturn;
        }

        public delegate void Callback(Cell aCell, SceneNode aNode);
        public event Callback OnUpdateBegin;
        public event Callback OnUpdateEnd;
    }
}
