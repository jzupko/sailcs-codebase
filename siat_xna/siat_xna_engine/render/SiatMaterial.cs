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
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.render
{
    public interface IMaterialParameter
    {
        int GetId();
        void SetToEffect(SiatEffect aEffect);
        bool Validate(SiatEffect aEffect);
    }

    #region Concrete definitions
    public abstract class MaterialParameter<T> 
    {
        #region Private members
        protected int mId;
        protected T mValue;
        #endregion

        public int GetId()
        {
            return mId;
        }

        public MaterialParameter(string aSemantic, T aValue)
        {
            mId = RenderRoot.GetParameterId(aSemantic);
            mValue = aValue;
        }

        public bool Validate(SiatEffect aEffect)
        {
            return (aEffect[mId] != null);
        }
    }

    public sealed class MaterialParameterSingle : MaterialParameter<float>, IMaterialParameter
    {
        public MaterialParameterSingle(string aSemantic, float aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }

    public sealed class MaterialParameterTexture : MaterialParameter<Texture>, IMaterialParameter
    {
        public MaterialParameterTexture(string aSemantic, Texture aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }

    public sealed class MaterialParameterMatrix : MaterialParameter<Matrix>, IMaterialParameter
    {
        public MaterialParameterMatrix(string aSemantic, Matrix aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }

    public sealed class MaterialParameterVector2 : MaterialParameter<Vector2>, IMaterialParameter
    {
        public MaterialParameterVector2(string aSemantic, Vector2 aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }

    public sealed class MaterialParameterVector3 : MaterialParameter<Vector3>, IMaterialParameter
    {
        public MaterialParameterVector3(string aSemantic, Vector3 aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }

    public sealed class MaterialParameterVector4 : MaterialParameter<Vector4>, IMaterialParameter
    {
            public MaterialParameterVector4(string aSemantic, Vector4 aValue)
            : base(aSemantic, aValue)
        { }

        public void SetToEffect(SiatEffect aEffect)
        {
            aEffect[mId].SetValue(mValue);
        }
    }
    #endregion

    /// <summary>
    /// Encapsulates Effect parameters.
    /// </summary>
    public sealed class SiatMaterial
    {
        #region Private members
        private List<IMaterialParameter> mParameters = new List<IMaterialParameter>();
        #endregion

        public void AddParameter(string aSemantic, float aValue)
        {
            mParameters.Add(new MaterialParameterSingle(aSemantic, aValue));
        }

        public void AddParameter(string aSemantic, Texture aValue)
        {
            mParameters.Add(new MaterialParameterTexture(aSemantic, aValue));
        }

        public void AddParameter(string aSemantic, Matrix aValue)
        {
            mParameters.Add(new MaterialParameterMatrix(aSemantic, aValue));
        }

        public void AddParameter(string aSemantic, Vector2 aValue)
        {
            mParameters.Add(new MaterialParameterVector2(aSemantic, aValue));
        }

        public void AddParameter(string aSemantic, Vector3 aValue)
        {
            mParameters.Add(new MaterialParameterVector3(aSemantic, aValue));
        }

        public void AddParameter(string aSemantic, Vector4 aValue)
        {
            mParameters.Add(new MaterialParameterVector4(aSemantic, aValue));
        }

        public void RemoveParameter(int aParameterId)
        {
            int count = mParameters.Count;
            for (int i = 0; i < count; i++)
            {
                if (mParameters[i].GetId() == aParameterId)
                {
                    mParameters.RemoveAt(i);
                    return;
                }
            }
        }

        public void SetToEffect(SiatEffect aEffect)
        {
            int count = mParameters.Count;
            for (int i = 0; i < count; i++)
            {
                mParameters[i].SetToEffect(aEffect);
            }
        }

        public void GetValidateFail(SiatEffect aEffect, List<string> aSemantics)
        {
            if (!Validate(aEffect))
            {
                int count = mParameters.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!mParameters[i].Validate(aEffect))
                    {
                        aSemantics.Add(RenderRoot.GetParameterSemantic(mParameters[i].GetId()));
                    }
                }
            }
        }

        public bool Validate(SiatEffect aEffect)
        {
            int count = mParameters.Count;
            for (int i = 0; i < count; i++)
            {
                if (!mParameters[i].Validate(aEffect))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
