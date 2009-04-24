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

namespace siat.render
{
    public sealed class AnimationControl
    {
        #region Private members
        private float mStartTime = 0.0f;
        private int mCurrentIndex = 0;
        private int mStartIndex = 0;
        private int mEndIndex = 0;
        private bool mbPlay = false;
        private bool mbDirty = true;

        internal bool Tick(Animation aAnimation, ref Matrix m)
        {
            if (aAnimation != null && (mbPlay || mbDirty))
            {
                bool bOk = (mStartIndex >= 0 &&
                            mStartIndex < mEndIndex &&
                            mEndIndex < aAnimation.KeyFrames.Length);
                
                mCurrentIndex = Utilities.Clamp(mCurrentIndex, mStartIndex, mEndIndex);

                float currentTime = (float)Siat.Singleton.Time.TotalGameTime.TotalSeconds;

                if (bOk && mbDirty)
                {
                    mStartTime = (currentTime - aAnimation.KeyFrames[mCurrentIndex].Time);
                    mbDirty = false;
                }

                float relTime = (currentTime - mStartTime);

                if (bOk)
                {
                    while (relTime > aAnimation.KeyFrames[mCurrentIndex + 1].Time)
                    {
                        mCurrentIndex++;

                        if (mCurrentIndex >= mEndIndex)
                        {
                            mCurrentIndex = mStartIndex;
                            mStartTime += (aAnimation.KeyFrames[mEndIndex].Time - aAnimation.KeyFrames[mStartIndex].Time);
                            relTime = (currentTime - mStartTime);
                        }
                    }

                    float lerp = Utilities.Clamp((relTime - aAnimation.KeyFrames[mCurrentIndex].Time) / (aAnimation.KeyFrames[mCurrentIndex + 1].Time - aAnimation.KeyFrames[mCurrentIndex].Time), 0.0f, 1.0f);
                    Matrix.Lerp(ref aAnimation.KeyFrames[mCurrentIndex].Key, ref aAnimation.KeyFrames[mCurrentIndex + 1].Key, lerp, out m);

                    return true;
                }
                else if (aAnimation.KeyFrames.Length > 0)
                {
                    m = aAnimation.KeyFrames[0].Key;
                    return true;
                }
            }

            return false;
        }
        #endregion

        public int CurrentIndex { get { return mCurrentIndex; } set { mCurrentIndex = value; mbDirty = true; } }
        public int StartIndex { get { return mStartIndex; } set { mStartIndex = value; mbDirty = true; } }
        public int EndIndex { get { return mEndIndex; } set { mEndIndex = value; mbDirty = true; } }
        public bool bPlay { get { return mbPlay; } set { if (value != mbPlay) { mbPlay = value; mCurrentIndex = mStartIndex; mbDirty = true; } } }
    }

    public sealed class Animation
    {
        #region Private members
        private AnimationKeyFrame[] mKeyFrames = new AnimationKeyFrame[0];
        #endregion

        public Animation(string aId)
        {
            Id = aId;
        }

        public readonly string Id;

        public AnimationKeyFrame[] KeyFrames { get { return mKeyFrames; } set { mKeyFrames = value; } }
    }
}
