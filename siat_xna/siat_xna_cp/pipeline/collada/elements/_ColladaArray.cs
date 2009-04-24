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
using System.Xml;

namespace siat.pipeline.collada.elements
{
    public abstract class _ColladaArray<T> : _ColladaElementWithIdAndName
    {
        #region Protected members
        protected readonly T[] mArray;
        protected readonly uint mCount = 0;
        #endregion

        public _ColladaArray(XmlReader aReader)
            : base(aReader)
        {
            #region Attributes
            _SetRequiredAttribute(aReader, Attributes.kCount, out mCount);
            mArray = new T[mCount];
            #endregion
        }

        public T[] Array { get { return mArray; }}
        public uint Count { get { return mCount; } }
        public T this[uint i] { get { return mArray[i]; } }

        public uint ElementCount
        {
            get
            {
                ColladaAccessor accessor = mParent.GetFirst<ColladaTechniqueCommonOfSource>()
                    .GetFirst<ColladaAccessor>();

                return accessor.Count;
            }
        }

        public Enums.ParamName[] Params
        {
            get
            {
                ColladaAccessor accessor = mParent.GetFirst<ColladaTechniqueCommonOfSource>()
                    .GetFirst<ColladaAccessor>();

                Enums.ParamName[] ret = new Enums.ParamName[accessor.GetChildCount<ColladaParam>()];

                int index = 0;
                foreach (ColladaParam p in accessor.GetEnumerable<ColladaParam>())
                {
                    ret[index++] = p.Name;
                }

                // Putting the check here because the relationship fo array/accessor elements
                // doesn't give me a better choice at the moment.
                if (ret.Length > Stride)
                {
                    throw new Exception("<*_array> has a stride that is lower than the number of <param> elements describing it.");
                }

                return ret;
            }
        }

        public uint Stride
        {
            get
            {
                uint ret = mParent.GetFirst<ColladaTechniqueCommonOfSource>()
                    .GetFirst<ColladaAccessor>().Stride;

                if (Count % ret != 0)
                {
                    throw new Exception("<*_array> has a count that is not evenly divisible by its stride.");
                }

                return ret;
            }
        }
                


    }
}
