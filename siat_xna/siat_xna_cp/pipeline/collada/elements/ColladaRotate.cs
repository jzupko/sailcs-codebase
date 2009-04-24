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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    public sealed class ColladaRotate : _ColladaTransformElement
    {
        #region Private members
        private readonly float mAngleInRadians = 0.0f;
        private readonly Vector3 mAxis = Vector3.Up;
        private readonly Quaternion mData;
        #endregion

        public const int kRotationParts = 4;

        public ColladaRotate(XmlReader aReader)
            : base(aReader)
        {
            #region Element values
            float[] buf = new float[kRotationParts];
            string value = "";
            _SetValue(aReader, ref value);
            Utilities.Tokenize(value, buf, XmlConvert.ToSingle);

            mAngleInRadians = MathHelper.ToRadians(buf[3]);
            mAxis = new Vector3(buf[0], buf[1], buf[2]);
            mData = Quaternion.CreateFromAxisAngle(mAxis, mAngleInRadians);
            #endregion
        }

        public Quaternion Rotation { get { return mData; } }
        public override Matrix XnaMatrix { get { return Matrix.CreateFromQuaternion(mData); } }

        public override AnimationKeyFrame[] GetKeyFrames(int index)
        {
            if (index >= TargetOf.Length)
            {
                return new AnimationKeyFrame[0];
            }
            else
            {
                List<AnimationKeyFrame> ret = new List<AnimationKeyFrame>();
                ColladaChannel channel = TargetOf[index];

                _ColladaArray<float> times = channel.Inputs;
                _ColladaArray<float> data = channel.Outputs;
                uint dataStride = data.Stride;
                Enums.ParamName[] prms = data.Params;
                uint paramStride = (uint)prms.Length;

                if (times.Stride != 1 || (data.Count / dataStride) != times.Count)
                {
                    throw new Exception("Invalid arrays for rotation key frame.");
                }

                uint count = times.Count;
                for (uint i = 0; i < count; i++)
                {
                    uint dataIndex = i * dataStride;
                    float angle = mAngleInRadians;
                    Vector3 axis = mAxis;
                    float time = times[i];
                    
                    for (uint j = dataIndex; j < dataIndex + paramStride; j++)
                    {
                        switch (prms[j - dataIndex])
                        {
                            case Enums.ParamName.kAngle: angle = MathHelper.ToRadians(data[j]); break;
                            case Enums.ParamName.kX: axis.X = data[j]; break;
                            case Enums.ParamName.kY: axis.Y = data[j]; break;
                            case Enums.ParamName.kZ: axis.Z = data[j]; break;
                            default:
                                throw new Exception("Invalid <param> name \"" + prms[j - dataIndex].ToString() + "\" for rotation key frame.");
                        }
                    }

                    ret.Add(new AnimationKeyFrame(time, Matrix.CreateFromAxisAngle(axis, angle)));
                }

                return ret.ToArray();
            }
        }
    }
}
