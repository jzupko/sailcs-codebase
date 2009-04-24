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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    /// <summary>
    /// This is a helper class to link targets of animation channels back to the animation channel.
    /// </summary>
    /// <remarks>
    /// This class is valid currently as the only animation resolution I am doing is skeletal animation.
    /// Eventually this may need to be further generalized to support texture animation, for example.
    /// </remarks>
    public abstract class _ColladaChannelTarget : _ColladaElementWithSid
    {
        #region Protected members
        protected List<ColladaChannel> mTargetOf = new List<ColladaChannel>();
        #endregion

        public _ColladaChannelTarget(XmlReader aReader)
            : base(aReader)
        { }

        public void AddTargetOf(ColladaChannel a) { mTargetOf.Add(a); }
        public ColladaChannel[] TargetOf { get { return mTargetOf.ToArray(); } }
        public bool IsTargetted { get { return (mTargetOf.Count != 0); } }

        /// <summary>
        /// If this element is a target of a "channel" element, this returns the key frames described
        /// by that channel element as an array of time/matrix pairs.
        /// </summary>
        /// <param name="i">The index of the channel target to get the key frames from.</param>
        /// <returns>Key frames as time/matrix pairs.</returns>
        public virtual AnimationKeyFrame[] GetKeyFrames(int i)
        {
            throw new Exception(Utilities.kNotImplemented);
        }
    }

}
