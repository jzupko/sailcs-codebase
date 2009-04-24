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

namespace siat.pipeline.collada.elements
{
    public sealed class ColladaAsset : _ColladaElement
    {
        #region Private members
        private readonly DateTime mCreated;
        private readonly string mKeywords = string.Empty;
        private readonly DateTime mModified;
        private readonly string mRevision = string.Empty;
        private readonly string mSubject = string.Empty;
        private readonly string mTitle = string.Empty;
        private readonly float mUnitMeter = Defaults.kUnitMeter;
        private readonly string mUnitName = Defaults.kUnitName;
        private readonly Vector3 mForwardAxis = Defaults.kForwardAxis;
        private readonly Vector3 mRightAxis = Defaults.kRightAxis;
        private readonly Vector3 mUpAxis = Defaults.kUpAxis;
        private readonly Enums.UpAxis mUpAxisType = Defaults.kUpAxisType;
        #endregion

        public ColladaAsset(XmlReader aReader)
        {
            #region Children
            _NextElement(aReader);
            _AddZeroToManyChildren(aReader, Elements.kContributor);
            _SetValueRequired(aReader, Elements.kCreated.Name, out mCreated);
            _SetValueOptional(aReader, Elements.kKeywords.Name, ref mKeywords);
            _SetValueRequired(aReader, Elements.kModified.Name, out mModified);
            _SetValueOptional(aReader, Elements.kRevision.Name, ref mRevision);
            _SetValueOptional(aReader, Elements.kSubject.Name, ref mSubject);
            _SetValueOptional(aReader, Elements.kTitle.Name, ref mTitle);
            _SetOptionalAttribute(aReader, Attributes.kUnitMeter, ref mUnitMeter);
            _SetOptionalAttribute(aReader, Attributes.kUnitName, ref mUnitName);
            _NextElement(aReader);
            #region up_axis
            string upAxis = string.Empty;
            if (_SetValueOptional(aReader, Elements.kUpAxis.Name, ref upAxis))
            {
                mUpAxisType = Enums.GetUpAxis(upAxis);

                switch (mUpAxisType)
                {
                    case Enums.UpAxis.kX: mForwardAxis = Vector3.UnitZ; mRightAxis = -Vector3.UnitY; mUpAxis = Vector3.UnitX; break;
                    case Enums.UpAxis.kY: mForwardAxis = Vector3.UnitZ; mRightAxis = Vector3.UnitX; mUpAxis = Vector3.UnitY; break;
                    case Enums.UpAxis.kZ: mForwardAxis = -Vector3.UnitY; mRightAxis = Vector3.UnitX; mUpAxis = Vector3.UnitZ; break;
                    default:
                        throw new Exception(Utilities.kShouldNotBeHere);
                }
            }
            #endregion
            #endregion        
        }

        public DateTime Created { get { return mCreated; } }
        public string Keywords { get { return mKeywords; } }
        public DateTime Modified { get { return mModified; } }
        public string Revision { get { return mRevision; } }
        public string Subject { get { return mSubject; } }
        public string Title { get { return mTitle; } }
        public float UnitMeter { get { return mUnitMeter; } }
        public string UnitName { get { return mUnitName; } }
        public Vector3 ForwardAxis { get { return mForwardAxis; } }
        public Vector3 RightAxis { get { return mRightAxis; } }
        public Vector3 UpAxis { get { return mUpAxis; } }

        /// <summary>
        /// Calculates a transform to convert the up axis of this ColladaAsset to a desired up axis.
        /// </summary>
        /// <param name="aDesiredUpAxis">The desired up axis.</param>
        /// <returns>The required transform.</returns>
        public Matrix GetXnaAxisTransform(Enums.UpAxis aDesiredUpAxis)
        {
            switch (mUpAxisType)
            {
                case Enums.UpAxis.kX:
                    switch (aDesiredUpAxis)
                    {
                        case Enums.UpAxis.kX:
                            return Matrix.Identity;
                        case Enums.UpAxis.kY:
                            return new Matrix( 0, 1, 0, 0,
                                              -1, 0, 0, 0,
                                               0, 0, 1, 0,
                                               0, 0, 0, 1);
                        case Enums.UpAxis.kZ:
                            return new Matrix( 0, 0, 1, 0,
                                              -1, 0, 0, 0,
                                               0, 1, 0, 0,
                                               0, 0, 0, 1);
                        default: goto end;
                    }
                case Enums.UpAxis.kY:
                    switch (aDesiredUpAxis)
                    {
                        case Enums.UpAxis.kX:
                            return new Matrix(0, -1, 0, 0,
                                              1,  0, 0, 0,
                                              0,  0, 1, 0,
                                              0,  0, 0, 1);
                        case Enums.UpAxis.kY:
                            return Matrix.Identity;
                        case Enums.UpAxis.kZ:
                            return new Matrix(1,  0, 0, 0,
                                              0,  0, 1, 0,
                                              0, -1, 0, 0,
                                              0,  0, 0, 1);
                        default: goto end;
                    }
                case Enums.UpAxis.kZ:
                    switch (aDesiredUpAxis)
                    {
                        case Enums.UpAxis.kX:
                            return new Matrix(0, -1,  0, 0,
                                              0,  0, -1, 0,
                                              1,  0,  0, 0,
                                              0,  0,  0, 1);
                        case Enums.UpAxis.kY:
                            return new Matrix(1, 0,  0, 0,
                                              0, 0, -1, 0,
                                              0, 1,  0, 0,
                                              0, 0,  0, 1);
                        case Enums.UpAxis.kZ:
                            return Matrix.Identity;
                        default: goto end;
                    }
                default:
                    goto end;
            }

            end: 
                throw new Exception(Utilities.kShouldNotBeHere);
        }
    }
}
