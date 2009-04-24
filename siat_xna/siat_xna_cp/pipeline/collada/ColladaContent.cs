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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System;
using System.Collections.Generic;
using System.Text;
using siat.pipeline.collada.elements;
using siat.pipeline.collada.elements.fx;

namespace siat.pipeline.collada
{
    /// <summary>
    /// Helper class used by ColladaProcessor.
    /// </summary>
    /// <remarks>
    /// DEPRECATED: soon to be folded into ColladaProcessor, no longer necessary.
    /// </remarks>
    public sealed class ColladaContent : ContentItem
    {
        public Dictionary<string, Dictionary<BoundEffect, SiatEffectContent>> Effects = new Dictionary<string, Dictionary<BoundEffect, SiatEffectContent>>();
        public Dictionary<string, SiatMaterialContent> Materials = new Dictionary<string, SiatMaterialContent>();
        public Dictionary<string, SiatMeshContent> Meshes = new Dictionary<string, SiatMeshContent>();
        public Dictionary<SiatMeshContent, PortalContent> Portals = new Dictionary<SiatMeshContent, PortalContent>();
        public ColladaScene Scene = null;

        public ColladaContent(string aSourceFilename)
            : base()
        {
            Identity = new ContentIdentity(aSourceFilename);
        }
    }

    public sealed class BoundEffect
    {
        public sealed class Binding
        {
            #region Private members
            private uint mHash;
            private string mSemantic;
            private uint mUsageIndex;

            private void _CalculateHash()
            {
                byte[] b = Encoding.UTF8.GetBytes(Semantic);
                mHash = Hash.Calculate32(b, mUsageIndex);
            }
            #endregion

            public Binding(string aSemantic, uint aUsageIndex)
            {
                mSemantic = aSemantic;
                mUsageIndex = aUsageIndex;
                _CalculateHash();
            }

            public static bool operator ==(Binding a, Binding b)
            {
                return (a.mSemantic == b.mSemantic) && (a.mUsageIndex == b.mUsageIndex);
            }

            public static bool operator !=(Binding a, Binding b)
            {
                return (a.mSemantic != b.mSemantic) && (a.mUsageIndex != b.mUsageIndex);
            }

            public static bool operator ==(Binding a, string s)
            {
                return (a.mSemantic == s);
            }

            public static bool operator !=(Binding a, string s)
            {
                return (a.mSemantic != s);
            }

            public static bool operator ==(string s, Binding a)
            {
                return (a.mSemantic == s);
            }

            public static bool operator !=(string s, Binding a)
            {
                return (a.mSemantic != s);
            }

            public static bool operator <(Binding a, Binding b)
            {
                return (a.mSemantic.CompareTo(b.mSemantic) < 0) && (a.mUsageIndex < b.mUsageIndex);
            }

            public static bool operator >(Binding a, Binding b)
            {
                return (a.mSemantic.CompareTo(b.mSemantic) > 0) && (a.mUsageIndex > b.mUsageIndex);
            }

            public override bool Equals(object obj)
            {
                if (obj is Binding)
                {
                    Binding b = (Binding)obj;
                    return (b.Semantic == Semantic) && (b.UsageIndex == UsageIndex);
                }
                else if (obj is string)
                {
                    string b = (string)obj;
                    return (b == Semantic);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (int)mHash;
            }

            public override string ToString()
            {
                return (mSemantic + mUsageIndex.ToString());
            }

            public string Semantic
            {
                get
                {
                    return mSemantic;
                }

                set
                {
                    mSemantic = value;
                    _CalculateHash();
                }
            }

            public uint UsageIndex
            {
                get
                {
                    return mUsageIndex;
                }

                set
                {
                    mUsageIndex = value;
                    _CalculateHash();
                }
            }
        }

        public List<Binding> Bindings = new List<Binding>();

        public BoundEffect(ColladaInstanceMaterial m)
        {
            foreach (ColladaBindVertexInput e in m.GetEnumerable<ColladaBindVertexInput>())
            {
                ColladaBindVertexInput binding = (ColladaBindVertexInput)e;
                Bindings.Add(new Binding(binding.Semantic, binding.InputSet));
            }
        }

        public override string ToString()
        {
            string ret = string.Empty;
            foreach (Binding b in Bindings)
            {
                ret += "_" + b.ToString();
            }

            return ret;
        }

        public override bool Equals(object obj)
        {
            if (obj is BoundEffect)
            {
                BoundEffect bound = (BoundEffect)obj;

                if (bound.Bindings.Count != Bindings.Count)
                {
                    return false;
                }
                else
                {
                    int count = Bindings.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (bound.Bindings[i] != Bindings[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (Bindings.Count == 0)
            {
                return 0;
            }
            else
            {
                int count = Bindings.Count;
                byte[] hashData = new byte[count * sizeof(int)];
                for (int i = 0; i < count; i++)
                {
                    byte[] val = BitConverter.GetBytes(Bindings[i].GetHashCode());
                    Array.Copy(val, 0, hashData, (i * sizeof(int)), sizeof(int));
                }

                uint hash = Hash.Calculate32(hashData, 0u);
                return (int)hash;
            }
        }

        public static bool operator <(BoundEffect a, BoundEffect b)
        {
            int count = Utilities.Min(a.Bindings.Count, b.Bindings.Count);
            for (int i = 0; i < count; i++)
            {
                if (a.Bindings[i] > b.Bindings[i])
                {
                    return false;
                }
                else if (a.Bindings[i] < b.Bindings[i])
                {
                    return true;
                }
            }

            return false;
        }

        public static bool operator >(BoundEffect a, BoundEffect b)
        {
            int count = Utilities.Min(a.Bindings.Count, b.Bindings.Count);
            for (int i = 0; i < count; i++)
            {
                if (a.Bindings[i] < b.Bindings[i])
                {
                    return false;
                }
                else if (a.Bindings[i] > b.Bindings[i])
                {
                    return true;
                }
            }

            return false;
        }

        public bool FindUsageIndex(string aSemantic, ref uint arOut)
        {
            foreach (Binding b in Bindings)
            {
                if (b == aSemantic)
                {
                    arOut = b.UsageIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
