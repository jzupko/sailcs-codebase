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
    /// <summary>
    /// Encapsulates a COLLADA "Name_array" element.
    /// </summary>
    /// <remarks>
    /// Note that unlike most arrays, this uses type object. This is becaue a "Name_array" can either
    /// be a vanilla string array or it may be a "Name_array" of sids to resolve to elements (this is
    /// used in animation to refer to joints). The distinction is made by the enclosing context at the 
    /// source element level, so the member ColladaNameArray.ResolveToSids() is provided and is
    /// expected to be called by an enclosing member.
    /// </remarks>
    public sealed class ColladaNameArray : _ColladaArray<object>
    {
        #region Private members
        private bool mbSids = false;
        private void _ParseSidsHelper(int aIndex, string aSid)
        {
            ColladaDocument.QueueSidForResolution(aSid, delegate(_ColladaElement a) { mArray[aIndex] = a; });
        }

        private void _ParseSids()
        {
            for (int i = 0; i < mCount; i++)
            {
                _ParseSidsHelper(i, (string)mArray[i]);
            }

            mbSids = true;
        }
        #endregion

        public ColladaNameArray(XmlReader aReader)
            : base(aReader)
        {
            #region Element value
            string value = string.Empty;
            string[] sids = new string[mCount];

            _SetValue(aReader, ref value);
            Utilities.Tokenize(value, sids);

            System.Array.Copy(sids, mArray, mCount);
            #endregion
        }

        public void ResolveToSids() { if (!mbSids) { _ParseSids(); } }
    }
}
