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
using System.Xml;

namespace siat.pipeline.collada.elements.fx
{
    public sealed class ColladaSetparamOfInstanceEffect : _ColladaElement
    {
        #region Private members
        private readonly string mRef = string.Empty;
        private readonly Enums.CoreValueType mType;
        private object mValue;

        private void _HandleValue(XmlReader aReader)
        {
            XmlReader sub = _Sub(aReader);

            float[] val = null;

            switch (mType)
            {
                case Enums.CoreValueType.kFloat:
                    _NextText(sub);
                    mValue = XmlConvert.ToSingle(sub.Value); break;
                case Enums.CoreValueType.kFloat2:
                    _NextText(sub);
                    val = Utilities.Tokenize<float>(sub.Value, XmlConvert.ToSingle);
                    mValue = new Vector2(val[0], val[1]);
                    break;
                case Enums.CoreValueType.kFloat3:
                    _NextText(sub);
                    val = Utilities.Tokenize<float>(sub.Value, XmlConvert.ToSingle);
                    mValue = new Vector3(val[0], val[1], val[2]);
                    break;
                case Enums.CoreValueType.kFloat4:
                    _NextText(sub);
                    val = Utilities.Tokenize<float>(sub.Value, XmlConvert.ToSingle);
                    mValue = new Vector4(val[0], val[1], val[2], val[3]);
                    break;
                case Enums.CoreValueType.kFloat4x4:
                    _NextText(sub);
                    val = Utilities.Tokenize<float>(sub.Value, XmlConvert.ToSingle);
                    mValue = ToXnaMatrix(val);
                    break;
                case Enums.CoreValueType.kSurface:
                    mValue = new ColladaSurfaceOfProfileCOMMON(sub);
                    break;
                default:
                    throw new Exception("Core value type \"" + Enum.GetName(typeof(Enums.CoreValueType), mType) +
                        "\" is not currently supported by importer.");
            }

            _NextElement(sub);
            _CheckSubFinished(sub);
            _NextElement(aReader);
        }
        #endregion

        public ColladaSetparamOfInstanceEffect(XmlReader aReader)
        {
            #region Attributes
            _SetRequiredAttribute(aReader, Attributes.kReference, out mRef);
            #endregion

            #region Children
            _NextElement(aReader);

            mType = Enums.GetCoreValueType(aReader.Name);
            _HandleValue(aReader);
            #endregion
        }

        public T GetValue<T>() { return (T)mValue; }
        public string Reference { get { return mRef; } }
        public Enums.CoreValueType Type { get { return mType; } }
        public object Value { get { return mValue; } }
    }
}
