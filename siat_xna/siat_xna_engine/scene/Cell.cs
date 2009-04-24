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

using SiatId = System.IntPtr;
using SiatRlt = System.Int32;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Threading;
using siat.render;

namespace siat.scene
{
    /// <summary>
    /// An axis-aligned bounding region of space that subdivides the world.
    /// </summary>
    /// <remarks>
    /// By default the engine is designed to subdivide the world into cells. Each Cell is
    /// an isolated region of space. Cells can only access other cells through portals, encapsulated
    /// by a PortalNode. 
    /// 
    /// A Cell consists of shared content (for example, siat.render.SiatEffect objects or siat.render.MeshPart
    /// objects) and a scene graph. Cells are deserialized from COLLADA .dae files. One Cell is equal to one scene
    /// specified in a COLLADA .dae file.
    /// 
    /// Cells are not only a way of visibly dividing the world. They also have their own XNA ContentManager.
    /// Cells load and unload content automatically in a separate thread to the main thread as needed.
    /// As a result, content loaded by a Cell (such as a siat.render.SiatEffect object) should not be used
    /// by other Cells. If content must cross Cell boundaries (such as the geometry of a main character avatar),
    /// this needs to be specially handled by the client application. One possibility is to use the global 
    /// ContentManager Siat.Content and to load shared content into it, although this shared content can
    /// never be unloaded. Another option would be to maintain several global ContentManager objects that
    /// are unloaded by the client application when appropriate.
    /// </remarks>
    /// 
    /// \sa siat.Program.Go
    /// \sa siat.scene.SceneNode
    /// \sa siat.render.SiatEffect
    /// \sa siat.render.MeshPart
    /// 
    /// \todo Cells currently do not serialize any changes to their scene graph that occur at runtime.
    ///       As a result, if a Cell is cached to disk and then loaded, the original COLLADA file is loaded
    ///       and any changes are lost. A possible solution to this is to separate shared content and
    ///       the scene graph of a Cell into two files. The file with shared content is always loaded while
    ///       the file with the scene graph can be preempted by a more up-to-date file written at runtime.
    /// 
    /// \todo Physics integration needs to be restructured. I'm thinking of moving physics completely out
    ///       of Cell to lighten the class and further decouple scene nodes from cells.
    /// 
    /// <h2>Examples</h2>
    /// <code>
    /// Cell cell = Cell.GetCell("my_cell_file.dae");
    /// 
    /// // WaitForRootSceneNode is a blocking call while RootSceneNode is threaded and may return null.
    /// // SceneNode root = cell.RootSceneNode;
    /// SceneNode root = cell.WaitForRootSceneNode;
    /// </code>
    public sealed class Cell : IPoseable
    {
        public const int kKdTreeDepth = OcclusionKdTree.kMaximumDepth; 

        #region Private members
        private static Dictionary<string, Cell> msCells = new Dictionary<string, Cell>();

        private bool mbLoading = false;
        private ContentManager mContent = new ContentManager(Siat.Singleton.Services, Utilities.kMediaRoot);
        private readonly string mFilename;
        private Matrix mCellToWorldTransform = Matrix.Identity;
        private Matrix mInverseCellToWorldTransform = Matrix.Identity;
        private uint mLastPickFrameTick = 0;
        private uint mLastPoseFrameTick = 0;
        private uint mLastUpdateFrameTick = 0;
        private OcclusionKdTree mKdTree;
        private SceneNode mRootSceneNode;
        private BoundingBox mWorldBounding = new BoundingBox();

        private void _HandleLoadMainThread()
        {
            mKdTree = new OcclusionKdTree(kKdTreeDepth);
            mRootSceneNode.Update(this, ref Utilities.kIdentity, true);
            mKdTree.Build();
            mWorldBounding = mKdTree.RootAABB;
        }

        #region HandleLoad
        private void __HandleLoad(object aObject)
        {
            lock (this)
            {
                if (mRootSceneNode == null) { mRootSceneNode = mContent.Load<SceneNode>(mFilename); }
            }
        }
        private WaitCallback _HandleLoad;
        #endregion

        private void _Load()
        {
            lock (this)
            {
                if (mRootSceneNode == null)
                {
                    if (!mbLoading)
                    {
                        mbLoading = ThreadPool.QueueUserWorkItem(_HandleLoad);
                    }
                }
                else if (mKdTree == null)
                {
                    _HandleLoadMainThread();
                    mbLoading = false;
                }
            }
        }

        private SceneNode _LoadImmediate()
        {
            lock (this)
            {
                if (mRootSceneNode == null)
                {
                    if (!mbLoading)
                    {
                        mbLoading = true;
                        {
                            mRootSceneNode = mContent.Load<SceneNode>(mFilename);
                            _HandleLoadMainThread();
                        }
                        mbLoading = false;
                    }
                }

                return mRootSceneNode;
            }
        }

        private void _Unload()
        {
            lock (this)
            {
                if (mKdTree != null)
                {
                    mKdTree = null;
                }

                if (mRootSceneNode != null)
                {
                    mContent.Unload();
                    mRootSceneNode = null;
                }
            }
        }

        private Cell(string aFilename)
        {
            mFilename = aFilename;
            _HandleLoad = __HandleLoad;
        }
        #endregion

        #region Internal members
        internal void Add(PoseableNode aNode)
        {
            if (mKdTree != null) mKdTree.Add(aNode);
        }

        internal void Update(PoseableNode aNode)
        {
            if (mKdTree != null) mKdTree.Update(aNode);
        }
        #endregion

        public static Cell GetCell(string aFilename)
        {
            string filename = Utilities.RemoveExtension(aFilename);
            Cell ret = null;

            if (!msCells.TryGetValue(filename, out ret))
            {
                ret = new Cell(filename);
                msCells.Add(filename, ret);
            }

            return ret;
        }

        public static void RefreshAll()
        {
            foreach (Cell e in msCells.Values)
            {
                lock (e)
                {
                    if (e.mRootSceneNode != null)
                    {
                        e.mRootSceneNode.Update(e, ref e.mCellToWorldTransform, true);
                    }
                }
            }
        }

        public static void UnloadAll()
        {
            foreach (Cell e in msCells.Values)
            {
                e._Unload();
            }
        }

        public Matrix CellToWorld { get { return mCellToWorldTransform; } }
        public Matrix InverseCellToWorld { get { return mInverseCellToWorldTransform; } }
        public SceneNode RootSceneNode { get { lock (this) { return mRootSceneNode; } } }
        public BoundingBox WorldBounding { get { return mWorldBounding; } }

        public SceneNode WaitForRootSceneNode
        {
            get
            {
                return _LoadImmediate();
            }
        }

        public void Pick(ref Ray aWorldRay)
        {
            Siat siat = Siat.Singleton;
            uint currentTick = siat.FrameTick;

            if (currentTick != mLastPickFrameTick)
            {
                mLastPickFrameTick = currentTick;

                if (mKdTree != null)
                {
                    mKdTree.Pick(this, ref aWorldRay);
                }
            }
        }

        /// <summary>
        /// Returns all PoseableNodes of type T whose bounding spheres intersect the point.
        /// </summary>
        /// <typeparam name="T">The child type of PoseableNode to return.</typeparam>
        /// <param name="aPoint">The point to intersect against.</param>
        /// <param name="aIntersections">The resultant intersections.</param>
        public void Query<T>(ref Vector3 aPoint, List<T> aIntersections)
            where T : PoseableNode
        {
            if (mKdTree != null) { mKdTree.Query<T>(ref aPoint, aIntersections); }
        }

        public void Query<T>(ref BoundingSphere aSphere, List<T> aIntersections)
            where T : PoseableNode
        {
            if (mKdTree != null) { mKdTree.Query<T>(ref aSphere, aIntersections); }
        }

#if DEBUG
        public int TotalQueriesIssued { get { if (mKdTree != null) { return mKdTree.TotalQueriesIssued; } else { return 0; } } }
#endif

        public void FrustumPose(IPoseable aPoseable)
        {
            Siat siat = Siat.Singleton;
            uint currentTick = siat.FrameTick;

            if (currentTick != mLastPoseFrameTick)
            {
                mLastPoseFrameTick = currentTick;

                if (mKdTree != null)
                {
                    mKdTree.FrustumPose(this);
                }
            }
        }

        public bool LightingPose(LightNode aLight)
        {
            if (mKdTree != null) { return mKdTree.LightingPose(aLight); }
            else { return false; }
        }

        public void ShadowingPose(LightNode aLight)
        {
            LightingPose(aLight);
        }

        public void Update(ref Matrix aCellToWorldTransform)
        {
            Siat siat = Siat.Singleton;
            uint currentTick = siat.FrameTick;

            if (mLastUpdateFrameTick != currentTick)
            {
                mLastUpdateFrameTick = currentTick;

                bool bUpdateWorld = false;

                if (!Utilities.AboutEqual(ref mCellToWorldTransform, ref aCellToWorldTransform, Utilities.kLooseToleranceFloat))
                {
                    mCellToWorldTransform = aCellToWorldTransform;
                    mInverseCellToWorldTransform = Matrix.Invert(mCellToWorldTransform);
                    bUpdateWorld = true;
                }

                _Load();

                lock (this)
                {
                    if (mRootSceneNode != null)
                    {
                        mRootSceneNode.Update(this, ref mCellToWorldTransform, bUpdateWorld);
                    }
                }

                if (mKdTree != null)
                {
                    if (bUpdateWorld)
                    {
                        mKdTree.Build();
                        mWorldBounding = mKdTree.RootAABB;
                    }

                    mKdTree.Tick();
                }
            }
        }
    }
}
