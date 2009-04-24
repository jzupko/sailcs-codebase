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
using System.Text;

namespace siat.scene
{
    /// <summary>
    /// A poseable object performs actions to prepare for rendering each frame.
    /// </summary>
    public interface IPoseable
    {
        /// <summary>
        /// This pose applies to poseables that want to be rendered as long as 
        /// they are in the current view Frustum.
        /// </summary>
        /// <param name="aPoseable">The poseable initiating this pose.</param>
        /// <remarks>
        /// Generally, if this poseable wants to initiate a new pose (a LightNode is 
        /// a poseable that responds to an IPoseable.FrustumPose() and as a result 
        /// initiates a new IPoseable.LightingPose() for example), it will do so by calling the
        /// appropriate pose function in aPoseable.
        /// </remarks>
        void FrustumPose(IPoseable aPoseable);

        /// <summary>
        /// This pose applies to poseables that want to be illuminated by LightNode aLight.
        /// </summary>
        /// <param name="aLight">The light initiating this pose.</param>
        /// <returns>True if any objects affected by this light are dirty.</returns>
        bool LightingPose(LightNode aLight);
    }

    /// <summary>
    /// A helper class to initiate poses on arbitrary scene graph hiearchies.
    /// </summary>
    /// <remarks>
    /// A pose for a scene graph is usually handled by the Cell that contains that scene graph. In cases
    /// where an arbitrary scene graph needs to be posed, this helper can be used. An instance of the helper
    /// should be instantiated with the root of the scene graph and SceneNodePoser.StartPose() called to
    /// begin a pose pass. SceneNodePoser.StartPose() should be called during the global pose pass which
    /// can be achieved by calling it from within a handler registered with Siat.OnPoseBegin() or 
    /// Siat.OnPoseEnd().
    /// </remarks>
    /// 
    /// \sa siat.Siat.OnPoseBegin()
    /// \sa siat.Siat.OnPoseEnd()
    /// \sa siat.scene.SceneNode
    /// 
    /// <h2>Examples</h2>
    /// <code>
    /// SceneNode mySceneContent;
    /// 
    /// ...
    /// 
    /// SceneNodePoser poser = new SceneNodePoser(mySceneContent);
    /// 
    /// ...
    /// 
    /// poser.StartPose();
    /// </code>
    public class SceneNodePoser : IPoseable
    {
        #region Protected members
        SceneNode mNode;

        private void _FrustumPose(IPoseable aPoseable, SceneNode aNode)
        {
            if (aNode is PoseableNode)
            {
                ((PoseableNode)aNode).FrustumPose(aPoseable);
            }

            for (SceneNode e = aNode.FirstChild; e != null; e = e.NextSibling)
            {
                _FrustumPose(aPoseable, e);
            }
        }

        private bool _LightingPose(LightNode aLight, SceneNode aNode)
        {
            bool bReturn = false;

            if (aNode is PoseableNode)
            {
                PoseableNode node = (PoseableNode)aNode;

                bReturn = bReturn || node.bMyShadowRequiresUpdate;
                node.LightingPose(aLight);
            }

            for (SceneNode e = aNode.FirstChild; e != null; e = e.NextSibling)
            {
                bReturn = _LightingPose(aLight, e) || bReturn;
            }

            return bReturn;
        }
        #endregion

        #region Overrides
        public void FrustumPose(IPoseable aPoseable) { _FrustumPose(aPoseable, mNode); }
        public bool LightingPose(LightNode aLight) { return _LightingPose(aLight, mNode); }
        #endregion

        public SceneNodePoser(SceneNode aNode) { mNode = aNode; }

        public SceneNode Node { get { return mNode; } set { mNode = value; } }
        public void StartPose() { FrustumPose(this); }
    };

}
