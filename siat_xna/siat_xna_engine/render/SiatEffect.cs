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

namespace siat.render
{
    [Flags]
    public enum SiatEffectFlags
    {
        None = 0,
        IsAnimatedBase = (1 << 0),
        IsAnimatedLightable = (1 << 1),
        IsStandardBase = (1 << 2),
        IsStandardLightable = (1 << 3),
        IsTransparent = (1 << 4),
        IsTransparentTexture = (1 << 5),
        NeedsBasePass = (1 << 6)
    }

    /// <summary>
    /// An effect that determines the material properties and render states used to draw geometry.
    /// </summary>
    /// <remarks>
    /// SiatEffect is a wrapper around XNA Effect. It automatically calculates the following flags
    /// about a contained XNA Effect, useful for determining the capabilities of a particular effect:
    /// - StandardBase - the contained Effect has the parameters and techniques necessary to be used
    ///                  for a base pass during rendering. 
    /// - StandardLightable - the contained Effect has the parameters and techniques necessary to be used
    ///                       for an illuminated pass, affected by a point, directional, spot, or shadowed
    ///                       spot light.
    /// - AnimatedBase - the contained Effect has the parameters and techniques necessary to be used
    ///                  for an animated base pass during rendering.
    /// - AnimatedLightable - the contained Effect has the parameters and techniques necessary to be used
    ///                       for an animated illuminated pass, affected by a point, directional, spot, or shadowed
    ///                       spot light.
    /// - IsTransparent - the contained Effect is transparent.
    /// - IsTransparentTexture - the contained Effect has a transparent texture.
    /// 
    /// In addition to exposing flags for a contained XNA Effect, SiatEffect also maintains a global table
    /// of Effect parameters and techniques by name, which is used to allow parameters to be universally
    /// accessible by index. Parameters for base and lightable passes have constant indices as defined
    /// in RenderRoot.BuiltInParameters, RenderRoot.BuiltInTechniques, 
    /// </remarks>
    /// 
    /// \sa siat.render.RenderRoot.BuiltInParameters
    /// \sa siat.render.RenderRoot.BuiltInTechniques
    public sealed class SiatEffect : IDisposable
    {
        public const string kAmbientColorSemantic = "siat_AmbientColor";
        public const string kAmbientTextureSemantic = "siat_AmbientTexture";
        public const string kEmissionColorSemantic = "siat_EmissionColor";
        public const string kEmissionTextureSemantic = "siat_EmissionTexture";

        #region Private members
        private readonly string mId;
        private EffectPassCollection mActivePasses = null;
        private int mActiveTechnique;
        private Effect mEffect;
        private SiatEffectFlags mFlags = SiatEffectFlags.None;
        private EffectParameter[] mParameterTable = new EffectParameter[0];
        private EffectTechnique[] mTechniqueTable = new EffectTechnique[0];

        private void _SetFlags()
        {
            #region Transparent
            {
                EffectParameter transparent = (this)[RenderRoot.BuiltInParameters.siat_Transparency];
                if (transparent != null) { mFlags |= SiatEffectFlags.IsTransparent; }
                else { mFlags &= ~SiatEffectFlags.IsTransparent; }
            }

            {
                EffectParameter transparentTexture = (this)[RenderRoot.BuiltInParameters.siat_TransparentTexture];
                if (transparentTexture != null) { mFlags |= SiatEffectFlags.IsTransparentTexture; }
                else { mFlags &= ~SiatEffectFlags.IsTransparentTexture; }
            }
            #endregion

            #region Needs base
            {
                if (mEffect.Parameters.GetParameterBySemantic(kAmbientColorSemantic) != null ||
                    mEffect.Parameters.GetParameterBySemantic(kAmbientTextureSemantic) != null ||
                    mEffect.Parameters.GetParameterBySemantic(kEmissionColorSemantic) != null ||
                    mEffect.Parameters.GetParameterBySemantic(kEmissionTextureSemantic) != null)
                {
                    mFlags |= SiatEffectFlags.NeedsBasePass;
                }
                else
                {
                    mFlags &= ~SiatEffectFlags.NeedsBasePass;
                }
            }
            #endregion

            #region IsAnimatedBase
            mFlags |= SiatEffectFlags.IsAnimatedBase;
            foreach (int i in RenderRoot.BuiltInParameters.kAnimatedBaseParameters)
            {
                if ((this)[i] == null)
                {
                    mFlags &= ~SiatEffectFlags.IsAnimatedBase;
                    mFlags &= ~SiatEffectFlags.IsAnimatedLightable;
                    break;
                }
            }
            foreach (object i in RenderRoot.BuiltInTechniques.kBaseTechniques)
            {
                if (GetTechnique(i) == null)
                {
                    mFlags &= ~SiatEffectFlags.IsAnimatedBase;
                    mFlags &= ~SiatEffectFlags.IsAnimatedLightable;
                    break;
                }
            }
            #endregion

            #region IsAnimatedLightable
            if (IsAnimatedBase)
            {
                mFlags |= SiatEffectFlags.IsAnimatedLightable;
                foreach (int i in RenderRoot.BuiltInParameters.kAnimatedLightableParameters)
                {
                    if ((this)[i] == null)
                    {
                        mFlags &= ~SiatEffectFlags.IsAnimatedLightable;
                        break;
                    }
                }
                if (IsAnimatedLightable)
                {
                    foreach (object i in RenderRoot.BuiltInTechniques.kLightableTechniques)
                    {
                        if (GetTechnique(i) == null)
                        {
                            mFlags &= ~SiatEffectFlags.IsAnimatedLightable;
                            return;
                        }
                    }
                }
            }
            #endregion

            if (IsAnimatedBase || IsAnimatedLightable)
            {
                return;
            }

            #region IsStandardBase
            foreach (int i in RenderRoot.BuiltInParameters.kBaseParameters)
            {
                if ((this)[i] == null)
                {
                    mFlags &= ~SiatEffectFlags.IsStandardBase;
                    mFlags &= ~SiatEffectFlags.IsStandardLightable;
                    return;
                }
            }

            foreach (object i in RenderRoot.BuiltInTechniques.kBaseTechniques)
            {
                if (GetTechnique(i) == null)
                {
                    mFlags &= ~SiatEffectFlags.IsStandardBase;
                    mFlags &= ~SiatEffectFlags.IsStandardLightable;
                    return;
                }
            }

            mFlags |= SiatEffectFlags.IsStandardBase;
            #endregion

            #region IsStandardLightable
            foreach (int i in RenderRoot.BuiltInParameters.kLightableParameters)
            {
                if ((this)[i] == null)
                {
                    mFlags &= ~SiatEffectFlags.IsStandardLightable;
                    return;
                }
            }

            foreach (object i in RenderRoot.BuiltInTechniques.kLightableTechniques)
            {
                if (GetTechnique(i) == null)
                {
                    mFlags &= ~SiatEffectFlags.IsStandardLightable;
                    return;
                }
            }

            mFlags |= SiatEffectFlags.IsStandardLightable;
            #endregion
        }

        private void _UpdateParameterTable()
        {
            EffectParameterCollection parameters = mEffect.Parameters;
            int count = parameters.Count;

            for (int i = 0; i < count; i++)
            {
                EffectParameter param = parameters[i];
                int id = RenderRoot.GetParameterId(param.Semantic);

                if (id >= mParameterTable.Length)
                {
                    Array.Resize<EffectParameter>(ref mParameterTable, id + 1);
                }

                mParameterTable[id] = param;
            }
        }

        private void _UpdateTechniqueTable()
        {
            EffectTechniqueCollection techniques = mEffect.Techniques;
            int count = techniques.Count;

            for (int i = 0; i < count; i++)
            {
                EffectTechnique tech = techniques[i];
                int id = RenderRoot.GetTechniqueId(tech.Name);

                if (id >= mTechniqueTable.Length)
                {
                    Array.Resize<EffectTechnique>(ref mTechniqueTable, id + 1);
                }

                mTechniqueTable[id] = tech;
            }
        }
        #endregion

        public SiatEffect(string aId, Effect aEffect)
        {
            mId = aId;

            if (aEffect == null)
            {
                throw new Exception("XNA Effect of Siat Effect cannot be null.");
            }

            mEffect = aEffect;
            _UpdateParameterTable();
            _UpdateTechniqueTable();
            _SetFlags();

#if DEBUG
            /*
            int techniqueCount = mEffect.Techniques.Count;
            for (int i = 0; i < techniqueCount; i++)
            {
                if (!mEffect.Techniques[i].Validate())
                {
                    throw new Exception("Technique \"" + mEffect.Techniques[i].Name + "\" of effect \"" +
                        mId + "\" failed validation.");
                }
            }
             */
#endif

            mActiveTechnique = RenderRoot.GetTechniqueId(mEffect.Techniques[0].Name);
            mEffect.CurrentTechnique = mTechniqueTable[mActiveTechnique];
            mActivePasses = mEffect.CurrentTechnique.Passes;
        }

        public void Begin() { mEffect.Begin(); }
        public void CommitChanges() { mEffect.CommitChanges(); }
        public void Dispose() { mEffect.Dispose(); }
        public void End() { mEffect.End(); }
        public string Id { get { return mId; } }
        public bool IsAnimatedBase { get { return ((mFlags & SiatEffectFlags.IsAnimatedBase) != 0); } }
        public bool IsAnimatedLightable { get { return ((mFlags & SiatEffectFlags.IsAnimatedLightable) != 0); } }
        public bool IsStandardBase { get { return ((mFlags & SiatEffectFlags.IsStandardBase) != 0); } }
        public bool IsStandardLightable { get { return ((mFlags & SiatEffectFlags.IsStandardLightable) != 0); } }
        public bool IsTransparent { get { return ((mFlags & SiatEffectFlags.IsTransparent) != 0); } }
        public bool IsTransparentTexture { get { return ((mFlags & SiatEffectFlags.IsTransparentTexture) != 0); } }
        public bool NeedsBasePass { get { return ((mFlags & SiatEffectFlags.NeedsBasePass) != 0); } }
        public EffectPassCollection Passes { get { return mActivePasses; } }

        public int CurrentTechnique
        {
            get
            {
                return mActiveTechnique;
            }

            set
            {
                if (mActiveTechnique != value)
                {
                    mActiveTechnique = value;

                    EffectTechnique technique = mTechniqueTable[mActiveTechnique];
                    mEffect.CurrentTechnique = technique;
                    mActivePasses = technique.Passes;
                }
            }
        }

        public EffectTechnique GetTechnique(object o)
        {
            if ((int)o >= mTechniqueTable.Length)
            {
                return null;
            }
            else
            {
                return mTechniqueTable[(int)o];
            }
        }

        public EffectTechnique GetTechnique(int i)
        {
            if (i >= mTechniqueTable.Length)
            {
                return null;
            }
            else
            {
                return mTechniqueTable[i];
            }
        }

        public EffectParameter this[int i]
        {
            get
            {
                if (i >= mParameterTable.Length)
                {
                    return null;
                }
                else
                {
                    return mParameterTable[i];
                }
            }
        }
    }
}
