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

#define TRANSPARENT_TEXTURE_1_BIT

using siat.scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace siat.render
{
    /// <summary>
    /// Contains all functions that execute draw commands to the XNA rendering API.
    /// </summary>
    /// <remarks>
    /// All points of access to the XNA rendering API by core engine objects are located
    /// in RenderRoot. For example, a MeshPartNode calls functions in RenderRoot.PoseOperations
    /// to prepare itself for drawing. 
    /// </remarks>
    /// 
    /// \sa siat.scene.MeshPartNode
    public static class RenderRoot
    {
        public const float kDefaultGamma = 2.2f;

        public static bool IsOk(GraphicsDevice aGraphicsDevice)
        {
            bool bReturn =
                (aGraphicsDevice != null) &&
                (!aGraphicsDevice.IsDisposed) &&
                (aGraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal);

            return bReturn;
        }

        public static float Gamma
        {
            get
            {
                return msGamma;
            }
            
            set
            {
                msGamma = Utilities.Max(value, Utilities.kLooseToleranceFloat);
            }
        }

        #region Private members
        private static float msGamma = kDefaultGamma;

        internal static Siat msSiat = Siat.Singleton;
        internal static SiatEffect msActiveEffect = null;
        internal static Color msClearColor = Color.Gray;
        internal static GraphicsDevice msGraphics = null;
        internal static RenderState msRenderState = null;

        internal static List<LightNode> msDeferredLightList = new List<LightNode>();

        internal static RenderNode msRenderShadow = RenderNode.SpawnRoot();
        internal static RenderNode msRenderPicking = RenderNode.SpawnRoot();
        internal static RenderNode msRenderBaseDeferred = RenderNode.SpawnRoot();
        internal static RenderNode msRenderBaseOpaque = RenderNode.SpawnRoot();
        internal static RenderNode msRenderOcclusionQueries = RenderNode.SpawnRoot();
        internal static RenderNode msRenderDeferred = RenderNode.SpawnRoot();
        internal static RenderNode msRenderLitOpaque = RenderNode.SpawnRoot();
        internal static RenderNode msRenderTransparent = RenderNode.SpawnRoot();
        internal static RenderNode msRenderSky = RenderNode.SpawnRoot();

        private static List<string> msParameterTable = new List<string>();
        private static List<string> msTechniqueTable = new List<string>();
        #endregion

        #region Internal members
        internal static void _ResetTrees()
        {
            msDeferredLightList.Clear();
            msRenderShadow.Reset();
            msRenderPicking.Reset();
            msRenderBaseDeferred.Reset();
            msRenderBaseOpaque.Reset();
            msRenderOcclusionQueries.Reset();
            msRenderDeferred.Reset();
            msRenderLitOpaque.Reset();
            msRenderSky.Reset();
            msRenderTransparent.Reset();
        }
        #endregion

        public enum StencilMasks
        {
            kNoDeferred = (1 << 0),
            kDeferredMask = (1 << 1)
        }

        /// <summary>
        /// Effect parameters by semantic that are used internally by the engine.
        /// </summary>
        public static class BuiltInParameters
        {
            public static readonly int siat_Gamma;
            public static readonly int siat_InverseTransposeWorldTransform;
            public static readonly int siat_InverseViewTransform;
            public static readonly int siat_LightAttenuation;
            public static readonly int siat_LightDiffuse;
            public static readonly int siat_LightPositionOrDirection;
            public static readonly int siat_LightSpecular;
            public static readonly int siat_PickingColor;
            public static readonly int siat_ProjectionTransform;
            public static readonly int siat_SkinningTransforms;
            public static readonly int siat_ShadowRange;
            public static readonly int siat_ShadowTexture;
            public static readonly int siat_ShadowTransform;
            public static readonly int siat_SpotDirection;
            public static readonly int siat_SpotCutoffCosHalfAngle;
            public static readonly int siat_SpotFalloffExponent;
            public static readonly int siat_Transparency;
            public static readonly int siat_TransparentTexture;
            public static readonly int siat_ViewTransform;
            public static readonly int siat_ViewProjectionTransform;
            public static readonly int siat_WorldTransform;

            public static readonly int[] kAnimatedBaseParameters;
            public static readonly int[] kAnimatedLightableParameters;
            public static readonly int[] kBaseParameters;
            public static readonly int[] kLightableParameters;

            static BuiltInParameters()
            {
                siat_Gamma = RenderRoot.GetParameterId("siat_Gamma");
                siat_InverseTransposeWorldTransform = RenderRoot.GetParameterId("siat_InverseTransposeWorldTransform");
                siat_InverseViewTransform = RenderRoot.GetParameterId("siat_InverseViewTransform");
                siat_LightAttenuation = RenderRoot.GetParameterId("siat_LightAttenuation");
                siat_LightDiffuse = RenderRoot.GetParameterId("siat_LightDiffuse");
                siat_LightPositionOrDirection = RenderRoot.GetParameterId("siat_LightPositionOrDirection");
                siat_LightSpecular = RenderRoot.GetParameterId("siat_LightSpecular");
                siat_PickingColor = RenderRoot.GetParameterId("siat_PickingColor");
                siat_ProjectionTransform = RenderRoot.GetParameterId("siat_ProjectionTransform");
                siat_ShadowRange = RenderRoot.GetParameterId("siat_ShadowRange");
                siat_ShadowTexture = RenderRoot.GetParameterId("siat_ShadowTexture");
                siat_ShadowTransform = RenderRoot.GetParameterId("siat_ShadowTransform");
                siat_SkinningTransforms = RenderRoot.GetParameterId("siat_SkinningTransforms");
                siat_SpotDirection = RenderRoot.GetParameterId("siat_SpotDirection");
                siat_SpotCutoffCosHalfAngle = RenderRoot.GetParameterId("siat_SpotCutoffCosHalfAngle");
                siat_SpotFalloffExponent = RenderRoot.GetParameterId("siat_SpotFalloffExponent");
                siat_Transparency = RenderRoot.GetParameterId("siat_Transparency");
                siat_TransparentTexture = RenderRoot.GetParameterId("siat_TransparentTexture");
                siat_ViewTransform = RenderRoot.GetParameterId("siat_ViewTransform");
                siat_ViewProjectionTransform = RenderRoot.GetParameterId("siat_ViewProjectionTransform");
                siat_WorldTransform = RenderRoot.GetParameterId("siat_WorldTransform");

                kAnimatedBaseParameters = new int[]
                    { siat_Gamma,
                      siat_SkinningTransforms,
                      siat_ViewProjectionTransform, 
                      siat_WorldTransform};

                kAnimatedLightableParameters = new int[]
                    { siat_Gamma,
                      siat_InverseViewTransform, 
                      siat_InverseTransposeWorldTransform,
                      siat_LightAttenuation,
                      siat_LightDiffuse,
                      siat_LightPositionOrDirection,
                      siat_LightSpecular,
                      siat_ShadowRange,
                      siat_ShadowTexture,
                      siat_ShadowTransform,
                      siat_SpotDirection,
                      siat_SpotCutoffCosHalfAngle,
                      siat_SpotFalloffExponent };

                kBaseParameters = new int[]
                    { siat_Gamma,
                      siat_ViewProjectionTransform, 
                      siat_WorldTransform };

                kLightableParameters = new int[]
                    { siat_Gamma,
                      siat_InverseViewTransform, 
                      siat_InverseTransposeWorldTransform,
                      siat_LightAttenuation,
                      siat_LightDiffuse,
                      siat_LightPositionOrDirection,
                      siat_LightSpecular,
                      siat_ShadowRange,
                      siat_ShadowTexture,
                      siat_ShadowTransform,
                      siat_SpotDirection,
                      siat_SpotCutoffCosHalfAngle,
                      siat_SpotFalloffExponent };
            }
        }

        /// <summary>
        /// Effect techniques by name that are used internally by the engine.
        /// </summary>
        public static class BuiltInTechniques
        {
            public static readonly object siat_RenderBase;
            public static readonly object siat_RenderDeferred;
            public static readonly object siat_RenderDirectionalLight;
            public static readonly object siat_RenderOcclusionQuery;
            public static readonly object siat_RenderPicking;
            public static readonly object siat_RenderPointLight;
            public static readonly object siat_RenderPortal;
            public static readonly object siat_RenderShadowDepth;
            public static readonly object siat_RenderAnimatedShadowDepth;
            public static readonly object siat_RenderSolid;
            public static readonly object siat_RenderSpotLight;
            public static object siat_RenderSpotLightShadow;
            public static readonly object siat_RenderWireframe;

            public static readonly object[] kBaseTechniques;
            public static readonly object[] kLightableTechniques;

            static BuiltInTechniques()
            {
                bool bPS3 = (Siat.Singleton.GraphicsDevice.GraphicsDeviceCapabilities.PixelShaderVersion.Major >= 3);

                siat_RenderBase = RenderRoot.GetTechniqueId("siat_RenderBase");
                siat_RenderDeferred = RenderRoot.GetTechniqueId("siat_RenderDeferred");
                siat_RenderDirectionalLight = RenderRoot.GetTechniqueId("siat_RenderDirectionalLight");
                siat_RenderOcclusionQuery = RenderRoot.GetTechniqueId("siat_RenderOcclusionQuery");
                siat_RenderPicking = RenderRoot.GetTechniqueId("siat_RenderPicking");
                siat_RenderPointLight = RenderRoot.GetTechniqueId("siat_RenderPointLight");
                siat_RenderPortal = RenderRoot.GetTechniqueId("siat_RenderPortal");
                siat_RenderShadowDepth = RenderRoot.GetTechniqueId("siat_RenderShadowDepth");
                siat_RenderAnimatedShadowDepth = RenderRoot.GetTechniqueId("siat_RenderAnimatedShadowDepth");
                siat_RenderSolid = RenderRoot.GetTechniqueId("siat_RenderSolid");
                siat_RenderSpotLight = RenderRoot.GetTechniqueId("siat_RenderSpotLight");
                siat_RenderSpotLightShadow = (bPS3) ? RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Filtered") : RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Unfiltered"); 
                siat_RenderWireframe = RenderRoot.GetTechniqueId("siat_RenderWireframe");

                kBaseTechniques = new object[] 
                    { siat_RenderBase };

                kLightableTechniques = new object[]
                    { siat_RenderDirectionalLight,
                      siat_RenderPointLight,
                      siat_RenderSpotLight,
                      siat_RenderSpotLightShadow };
            }
        }

        static RenderRoot()
        {}

        public static Color ClearColor { get { return msClearColor; } set { msClearColor = value; } }

        public static void Draw()
        {
            DepthStencilBuffer defaultBuffer = msGraphics.DepthStencilBuffer;
            msRenderShadow.RenderChildrenAndReset();
            msGraphics.DepthStencilBuffer = defaultBuffer;

            if (Deferred.bActive)
            {
                Deferred.SetRenderTargets();
                msRenderDeferred.RenderChildrenAndReset();

                DeferredPost.Begin();
                msRenderBaseDeferred.RenderChildrenAndReset();
                Deferred.RenderLights(msDeferredLightList);
                msDeferredLightList.Clear();
            }
            else
            {
                ForwardPost.Begin();
                //msGraphics.SetRenderTarget(0, null);
                //msGraphics.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil | ClearOptions.Target, msClearColor, 1.0f, Siat.kDefaultReferenceStencil);
            }

            msRenderBaseOpaque.RenderChildrenAndReset();
            msRenderOcclusionQueries.RenderChildrenAndReset();
            msRenderLitOpaque.RenderChildrenAndReset();
            msRenderSky.RenderChildrenAndReset();
            msRenderTransparent.RenderChildrenAndReset();

            if (Deferred.bActive) { DeferredPost.End(); }
            else { ForwardPost.End(); }
        }

        public static bool bDeferredLighting
        {
            get { return Deferred.bActive; }
            set
            {
                if (value) { ForwardPost.OnUnload(); Deferred.Activate(); }
                else { Deferred.Deactivate(); ForwardPost.OnLoad(); }
            }
        }

        public static bool bFilteredShadows
        {
            get
            {
                return (int)BuiltInTechniques.siat_RenderSpotLightShadow == RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Filtered");
            }

            set
            {
                bool bPS3 = (Siat.Singleton.GraphicsDevice.GraphicsDeviceCapabilities.PixelShaderVersion.Major >= 3);

                if (bPS3)
                {
                    if (value) { BuiltInTechniques.siat_RenderSpotLightShadow = RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Filtered"); }
                    else { BuiltInTechniques.siat_RenderSpotLightShadow = RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Unfiltered"); }
                }
                else { BuiltInTechniques.siat_RenderSpotLightShadow = RenderRoot.GetTechniqueId("siat_RenderSpotLightShadow_Unfiltered"); }
            }
        }

        public static Color Pick()
        {
            msGraphics.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil | ClearOptions.Target, Siat.kPickClearColor, 1.0f, Siat.kDefaultReferenceStencil);
            msRenderPicking.RenderChildrenAndReset();
            msGraphics.ResolveBackBuffer(msSiat.mPickTexture);

            Color[] textureData = new Color[1];
            msSiat.mPickTexture.GetData<Color>(0, msSiat.mPickRectangle, textureData, 0, 1);
            Color pixel = textureData[0];

            return pixel;
        }

        public static int GetParameterId(string aParameterSemantic)
        {
            int count = msParameterTable.Count;
            for (int i = 0; i < count; i++)
            {
                if (msParameterTable[i] == aParameterSemantic)
                {
                    return i;
                }
            }

            msParameterTable.Add(aParameterSemantic);

            return (msParameterTable.Count - 1);
        }

        public static string GetParameterSemantic(int aId)
        {
            return msParameterTable[aId];
        }

        public static int ParameterCount
        {
            get
            {
                return msParameterTable.Count;
            }
        }

        public static int GetTechniqueId(string aTechniqueSemantic)
        {
            int count = msTechniqueTable.Count;
            for (int i = 0; i < count; i++)
            {
                if (msTechniqueTable[i] == aTechniqueSemantic)
                {
                    return i;
                }
            }

            msTechniqueTable.Add(aTechniqueSemantic);

            return (msTechniqueTable.Count - 1);
        }

        public static string GetTechniqueSemantic(int aId)
        {
            return msTechniqueTable[aId];
        }

        public static int TechniqueCount
        {
            get
            {
                return msTechniqueTable.Count;
            }
        }

        /// <summary>
        /// These functions are used to "pose" poseable scene node instances.
        /// </summary>
        /// <remarsk>
        /// Each render frame there are three phases in this order: Update, Pose, and Draw. The update phase
        /// recalculates the spatial transforms that define the position, orientation, and scale of scene
        /// nodes. This is also when any client action usually occurs (AI, user input). The pose phase finds
        /// any objects that are visible and submits them for render to RenderRoot. The draw phase actually
        /// executes the XNA API render operations to draw the objects each frame.
        /// </remarsk>
        public static class PoseOperations
        {
            #region Private members
            private static float _SortForOpaque(float aViewDepth) { return -aViewDepth; }
            private static float _SortForTransparent(float aViewDepth) { return aViewDepth; }

            private static void _GetLightDelegateAndTechnique(object aObject, out RenderNodeDelegate arDelegate, out object arTechnique, bool abCastShadow)
            {
                LightNode lightNode = (LightNode)aObject;

                switch (lightNode.Light.Type)
                {
                    case LightType.Spot:
                        arDelegate = (abCastShadow) ? RenderOperations.SpotLightShadow : RenderOperations.SpotLight;
                        arTechnique = (abCastShadow) ? BuiltInTechniques.siat_RenderSpotLightShadow : BuiltInTechniques.siat_RenderSpotLight;
                        break;
                    case LightType.Point:
                        arDelegate = RenderOperations.PointLight;
                        arTechnique = BuiltInTechniques.siat_RenderPointLight;
                        break;
                    default:
                        arDelegate = RenderOperations.DirectionalLight;
                        arTechnique = BuiltInTechniques.siat_RenderDirectionalLight;
                        break;
                }
            }

            #region Base
            private static void _MeshPartBaseOpaque(RenderNode aRoot, MatrixWrapper aWorld, Vector4[] aSkinning, float aOpaqueSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect)
            {
                RenderNode node = aRoot;
                node = node.AdoptAndUpdateSort(RenderOperations.Effect, aEffect, aOpaqueSort);
                node = node.Adopt(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderBase);
                node = node.AdoptAndUpdateSort(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration, aOpaqueSort);
                if (aMaterial != null) { node = node.AdoptAndUpdateSort(RenderOperations.Material, aMaterial, aOpaqueSort); }
                node = node.AdoptAndUpdateSort(RenderOperations.Mesh, aMeshPart, aOpaqueSort);
                if (aSkinning != null)
                {
                    node = node.AdoptSorted(RenderOperations.SkinningTransforms, aSkinning, aOpaqueSort);
                    node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
                }
                else
                {
                    node = node.AdoptSorted(RenderOperations.WorldTransformAndDrawIndexed, aWorld, aOpaqueSort);
                }
            }

            private static void _MeshPartDeferred(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aOpaqueSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, RenderNodeDelegate aStencilOp)
            {
                if (aEffect.GetTechnique(BuiltInTechniques.siat_RenderDeferred) == null) { return; }

                RenderNode node = msRenderDeferred;
                node = node.AdoptAndUpdateSort(RenderOperations.Effect, aEffect, aOpaqueSort);
                node = node.AdoptAndUpdateSort(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped, aOpaqueSort);
                node = node.AdoptAndUpdateSort(RenderOperations.ViewTransform, Shared.ViewTransformWrapped, aOpaqueSort);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderDeferred);
                node = node.AdoptAndUpdateSort(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration, aOpaqueSort);
                node = node.AdoptAndUpdateSort(aStencilOp, aStencilOp, aOpaqueSort);
                if (aMaterial != null) { node = node.AdoptAndUpdateSort(RenderOperations.Material, aMaterial, aOpaqueSort); }
                node = node.AdoptAndUpdateSort(RenderOperations.Mesh, aMeshPart, aOpaqueSort);
                if (aSkinning != null) { node.AdoptAndUpdateSort(RenderOperations.SkinningTransforms, aSkinning, aOpaqueSort); }

                if (aITWorld != null)
                {
                    node = node.AdoptSorted(RenderOperations.InverseTransposeWorldTransform, aITWorld, aOpaqueSort);
                    node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
                }
                else
                {
                    node = node.AdoptSorted(RenderOperations.WorldTransformAndDrawIndexed, aWorld, aOpaqueSort);
                }
            }

            private static void _MeshPartBaseTransparent(MatrixWrapper aWorld, Vector4[] aSkinning, float aTransparentSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect)
            {
                RenderNode node = msRenderTransparent;
                node = node.AdoptSorted(RenderOperations.Effect, aEffect, aTransparentSort);
                node = node.AdoptSorted(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped, 0.0f);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderBase);
                node = node.Adopt(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration);
                if (aMaterial != null) { node = node.Adopt(RenderOperations.Material, aMaterial); }
                node = node.Adopt(RenderOperations.Mesh, aMeshPart);
                if (aSkinning != null) { node = node.Adopt(RenderOperations.SkinningTransforms, aSkinning); }
                node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
            }

            private static void _MeshPartBase(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, bool abIncludeInDeferred)
            {
#if TRANSPARENT_TEXTURE_1_BIT
                if (aEffect.IsTransparent && !aEffect.IsTransparentTexture)
#else
                if (aEffect.IsTransparent)
#endif
                {
                    _MeshPartBaseTransparent(aWorld, aSkinning, _SortForTransparent(aViewDepth), aMeshPart, aMaterial, aEffect);
                }
                else
                {
                    if (Deferred.bActive)
                    {
                        if (abIncludeInDeferred)
                        {
                            _MeshPartDeferred(aWorld, aITWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect, RenderOperations.StencilDeferred);
                            if (aEffect.NeedsBasePass) { _MeshPartBaseOpaque(msRenderBaseDeferred, aWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect); }
                        }
                        else
                        {
                            _MeshPartDeferred(aWorld, aITWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect, RenderOperations.StencilNoDeferred);
                            _MeshPartBaseOpaque(msRenderBaseOpaque, aWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect);
                        }
                    }
                    else
                    {
                        _MeshPartBaseOpaque(msRenderBaseOpaque, aWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect);
                    }
                }
            }
            #endregion

            #region Lit
            private static void _MeshPartLitTransparent(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aTransparentSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aObject, bool abCastShadow)
            {
                RenderNode node = msRenderTransparent;

                RenderNodeDelegate lightDelegate;
                object technique;
                _GetLightDelegateAndTechnique(aObject, out lightDelegate, out technique, abCastShadow);

                node = node.AdoptSorted(RenderOperations.Effect, aEffect, aTransparentSort);
                node = node.AdoptSorted(RenderOperations.SetStandardEffectTransforms, Utilities.kDummy, 1.0f);
                node = node.Adopt(RenderOperations.EffectTechnique, technique);
                node = node.Adopt(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration);
                node = node.Adopt(lightDelegate, aObject);
                if (aMaterial != null) { node = node.Adopt(RenderOperations.Material, aMaterial); }
                node = node.Adopt(RenderOperations.Mesh, aMeshPart);
                if (aSkinning != null) { node = node.AdoptFront(RenderOperations.SkinningTransforms, aSkinning); }
                if (aITWorld != null) { node = node.AdoptFront(RenderOperations.InverseTransposeWorldTransform, aITWorld); }
                node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
            }

            private static void _MeshPartLitOpaque(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aOpaqueSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aObject, bool abCastShadow)
            {
                RenderNode node = msRenderLitOpaque;

                RenderNodeDelegate lightDelegate;
                object technique;
                _GetLightDelegateAndTechnique(aObject, out lightDelegate, out technique, abCastShadow);

                node = node.Adopt(RenderOperations.Effect, aEffect);
                node = node.Adopt(RenderOperations.SetStandardEffectTransforms, Utilities.kDummy);
                node = node.Adopt(RenderOperations.EffectTechnique, technique);
                node = node.Adopt(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration);
                node = node.Adopt(lightDelegate, aObject);
                if (aMaterial != null) { node = node.Adopt(RenderOperations.Material, aMaterial); }
                node = node.Adopt(RenderOperations.Mesh, aMeshPart);
                if (aSkinning != null) { node = node.AdoptFront(RenderOperations.SkinningTransforms, aSkinning); }
                if (aITWorld != null) { node = node.AdoptFront(RenderOperations.InverseTransposeWorldTransform, aITWorld); }
                node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
            }

            private static void _MeshPartLit(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aObject, bool abCastShadow, bool abIncludeInDeferred)
            {
                LightNode light = (LightNode)aObject;

#if TRANSPARENT_TEXTURE_1_BIT
                if (aEffect.IsTransparent && !aEffect.IsTransparentTexture)
#else
                if (aEffect.IsTransparent)
#endif
                {
                    _MeshPartLitTransparent(aWorld, aITWorld, aSkinning, _SortForTransparent(aViewDepth), aMeshPart, aMaterial, aEffect, aObject, abCastShadow);
                }
                else if (!(Deferred.bActive && abIncludeInDeferred))
                {
                    _MeshPartLitOpaque(aWorld, aITWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect, aObject, abCastShadow);
                }
            }
            #endregion

            private static void _MeshPartShadow(MatrixWrapper aWorld, Vector4[] aSkinning, object aShadowDepthTechnique, float aOpaqueSort, MeshPart aMeshPart, object aObject)
            {
                LightNode lightNode = (LightNode)aObject;

                RenderNode node = msRenderShadow;
                node = node.Adopt(RenderOperations.Effect, msSiat.BuiltInEffect);
                node = node.Adopt(RenderOperations.RenderTargetAndClear, lightNode.ShadowRenderTarget);
                node = node.Adopt(RenderOperations.ViewTransform, lightNode.ShadowViewWrapped);
                node = node.Adopt(RenderOperations.ViewProjectionTransform, lightNode.ShadowViewProjectionWrapped);
                node = node.Adopt(RenderOperations.ShadowRangeParameter, lightNode.RangeBoxed);
                node = node.AdoptAndUpdateSort(RenderOperations.EffectTechnique, aShadowDepthTechnique, aOpaqueSort);
                node = node.AdoptAndUpdateSort(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration, aOpaqueSort);
                node = node.AdoptAndUpdateSort(RenderOperations.Mesh, aMeshPart, aOpaqueSort);
                if (aSkinning != null) { node = node.AdoptAndUpdateSort(RenderOperations.SkinningTransforms, aSkinning, aOpaqueSort); }
                node = node.AdoptSorted(RenderOperations.WorldTransformAndDrawIndexed, aWorld, aOpaqueSort);
            }

            private static void _Picking(MatrixWrapper aWorld, Vector4[] aSkinning, float aOpaqueSort, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aPickColorBoxed)
            {
                RenderNode node = msRenderPicking;
                node = node.AdoptAndUpdateSort(RenderOperations.Effect, aEffect, aOpaqueSort);
                node = node.Adopt(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderPicking);
                node = node.AdoptAndUpdateSort(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration, aOpaqueSort);
                if (aMaterial != null) { node = node.AdoptAndUpdateSort(RenderOperations.Material, aMaterial, aOpaqueSort); }
                node = node.AdoptAndUpdateSort(RenderOperations.Mesh, aMeshPart, aOpaqueSort);
                if (aSkinning != null) { node = node.AdoptAndUpdateSort(RenderOperations.SkinningTransforms, aSkinning, aOpaqueSort); }
                node = node.AdoptFront(RenderOperations.PickColor, aPickColorBoxed);
                node = node.AdoptFront(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
            }
            #endregion

            public static void AnimatedMeshPartBase(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, bool abIncludeInDeferred)
            {
                _MeshPartBase(aWorld, aITWorld, aSkinning, aViewDepth, aMeshPart, aMaterial, aEffect, abIncludeInDeferred);
            }

            public static void AnimatedMeshPartLit(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aObject, bool abCastShadow, bool abIncludeInDeferred)
            {
                _MeshPartLit(aWorld, aITWorld, aSkinning, aViewDepth, aMeshPart, aMaterial, aEffect, aObject, abCastShadow, abIncludeInDeferred);
            }

            public static void AnimatedMeshPartShadow(MatrixWrapper aWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, object aObject)
            {
                _MeshPartShadow(aWorld, aSkinning, BuiltInTechniques.siat_RenderAnimatedShadowDepth, _SortForOpaque(aViewDepth), aMeshPart, aObject); 
            }

            public static void AnimatedPicking(MatrixWrapper aWorld, Vector4[] aSkinning, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aPickColorBoxed)
            {
                _Picking(aWorld, aSkinning, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect, aPickColorBoxed);
            }

            public static void MeshPartBase(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, bool abIncludeInDeferred)
            {
                _MeshPartBase(aWorld, aITWorld, null, aViewDepth, aMeshPart, aMaterial, aEffect, abIncludeInDeferred);
            }

            public static void MeshPartLit(MatrixWrapper aWorld, Matrix3Wrapper aITWorld, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aObject, bool abCastShadow, bool abIncludeInDeferred)
            {
                _MeshPartLit(aWorld, aITWorld, null, aViewDepth, aMeshPart, aMaterial, aEffect, aObject, abCastShadow, abIncludeInDeferred);
            }

            public static void MeshPartShadow(MatrixWrapper aWorld, float aViewDepth, MeshPart aMeshPart, object aObject)
            {
                _MeshPartShadow(aWorld, null, BuiltInTechniques.siat_RenderShadowDepth, _SortForOpaque(aViewDepth), aMeshPart, aObject);
            }

            public static void OcclusionQuery(MatrixWrapper aWorld, OcclusionQuery aOcclusionQuery)
            {
                RenderNode node;
                node = msRenderOcclusionQueries.Adopt(RenderOperations.Effect, msSiat.BuiltInEffect);
                node = node.Adopt(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderOcclusionQuery);
                node = node.Adopt(RenderOperations.VertexDeclaration, msSiat.UnitBoxMeshPart.VertexDeclaration);
                node = node.Adopt(RenderOperations.Mesh, msSiat.UnitBoxMeshPart);
                node = node.AdoptFront(RenderOperations.WorldTransform, aWorld);
                node = node.AdoptFront(RenderOperations.OcclusionQueryAndDrawIndexed, aOcclusionQuery);
            }

            public static void Picking(MatrixWrapper aWorld, float aViewDepth, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect, object aPickColorBoxed)
            {
                _Picking(aWorld, null, _SortForOpaque(aViewDepth), aMeshPart, aMaterial, aEffect, aPickColorBoxed);
            }

            /// <summary>
            /// Poses a sky for rendering.
            /// </summary>
            /// <param name="aWorldTransformWrapped">The world transform of the sky.</param>
            /// <param name="aMeshPart">Mesh of the sky.</param>
            /// <param name="aMaterial">Material of the sky.</param>
            /// <param name="aEffect">Effect of the sky</param>
            /// <remarks>
            /// Only one sky is allowed, so no sorting occurs in the sky render tree.
            /// </remarks>
            public static void Sky(MatrixWrapper aWorld, MeshPart aMeshPart, SiatMaterial aMaterial, SiatEffect aEffect)
            {
                if (Deferred.bActive && aEffect.GetTechnique(BuiltInTechniques.siat_RenderDeferred) != null)
                {
                    float sort = float.MaxValue;

                    RenderNode node = msRenderDeferred;
                    node = node.AdoptAndUpdateSort(RenderOperations.Effect, aEffect, sort);
                    node = node.AdoptAndUpdateSort(RenderOperations.ViewProjectionTransform, Shared.InfiniteViewProjectionTransformWrapped, sort);
                    node = node.AdoptAndUpdateSort(RenderOperations.ViewTransform, Shared.ViewTransformWrapped, sort);
                    node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderDeferred);
                    node = node.AdoptAndUpdateSort(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration, sort);
                    node = node.AdoptAndUpdateSort(RenderOperations.StencilNoDeferred, RenderOperations.StencilNoDeferred, sort);
                    if (aMaterial != null) { node = node.AdoptAndUpdateSort(RenderOperations.Material, aMaterial, sort); }
                    node = node.AdoptAndUpdateSort(RenderOperations.Mesh, aMeshPart, sort);
                    node = node.AdoptSorted(RenderOperations.WorldTransformAndDrawIndexed, aWorld, sort);
                }

                {
                    RenderNode node = msRenderSky;
                    node = node.Adopt(RenderOperations.Effect, aEffect);
                    node = node.Adopt(RenderOperations.ViewProjectionTransform, Shared.InfiniteViewProjectionTransformWrapped);
                    node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderBase);
                    node = node.Adopt(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration);
                    if (aMaterial != null) node = node.Adopt(RenderOperations.Material, aMaterial);
                    node = node.Adopt(RenderOperations.Mesh, aMeshPart);
                    node = node.Adopt(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
                }
            }

            public static void Wireframe(MatrixWrapper aWorld, MeshPart aMeshPart)
            {
                float sort = _SortForOpaque(Vector3.Transform(aWorld.Matrix.Translation, Shared.ViewTransform).Z);

                RenderNode node = msRenderBaseOpaque;
                node = node.AdoptAndUpdateSort(RenderOperations.Effect, msSiat.BuiltInEffect, sort);
                node = node.Adopt(RenderOperations.ViewProjectionTransform, Shared.ViewProjectionTransformWrapped);
                node = node.Adopt(RenderOperations.EffectTechnique, BuiltInTechniques.siat_RenderWireframe);
                node = node.Adopt(RenderOperations.VertexDeclaration, aMeshPart.VertexDeclaration);
                node = node.Adopt(RenderOperations.Mesh, aMeshPart);
                node = node.Adopt(RenderOperations.WorldTransformAndDrawIndexed, aWorld);
            }

            public static void WireframeBox(MatrixWrapper aWorld)
            {
                Wireframe(aWorld, msSiat.UnitBoxMeshPart);
            }

            public static void WireframeSphere(MatrixWrapper aWorld)
            {
                Wireframe(aWorld, msSiat.UnitSphereMeshPart);
            }
        }

        public static class RenderOperations
        {
            #region Private members
            private static void _DirectionalLight(RenderNode aNode, object aInstance)
            {
                LightNode lightNode = (LightNode)aInstance;
                Light light = lightNode.Light;
                msActiveEffect[BuiltInParameters.siat_LightDiffuse].SetValue(light.LightDiffuse);
                msActiveEffect[BuiltInParameters.siat_LightPositionOrDirection].SetValue(lightNode.WorldLightDirection);
                msActiveEffect[BuiltInParameters.siat_LightSpecular].SetValue(light.LightSpecular);

                aNode.RenderChildren();
            }

            private static void _Effect(RenderNode aNode, object aInstance)
            {
                msActiveEffect = (SiatEffect)aInstance;
                if (msActiveEffect[BuiltInParameters.siat_Gamma] != null)
                {
                    msActiveEffect[BuiltInParameters.siat_Gamma].SetValue(Gamma);
                }
                aNode.RenderChildren();
            }

            private static void _EffectTechnique(RenderNode aNode, object aInstance)
            {
                Siat siat = Siat.Singleton;
                msActiveEffect.CurrentTechnique = (int)aInstance;
                msActiveEffect.Begin();
                {
                    EffectPassCollection passes = msActiveEffect.Passes;
                    int count = passes.Count;
                    for (int i = 0; i < count; i++)
                    {
                        siat.mEffectPasses++;
                        passes[i].Begin();
                        {
                            aNode.RenderChildren();
                        }
                        passes[i].End();
                    }
                }
                msActiveEffect.End();
            }

            private static void _Material(RenderNode aNode, object aInstance)
            {
                SiatMaterial material = (SiatMaterial)aInstance;
                material.SetToEffect(msActiveEffect);

                aNode.RenderChildren();
            }

            private static void _Mesh(RenderNode aNode, object aInstance)
            {
                MeshPart part = (MeshPart)aInstance;

                msGraphics.Indices = part.Indices;
                msGraphics.Vertices[0].SetSource(part.Vertices, 0, part.VertexStride);
                msSiat.DrawIndexedSettings.PrimitiveType = part.PrimitiveType;
                msSiat.DrawIndexedSettings.BaseVertex = 0;
                msSiat.DrawIndexedSettings.MinVertexIndex = 0;
                msSiat.DrawIndexedSettings.NumberOfVertices = part.VertexCount;
                msSiat.DrawIndexedSettings.StartIndex = 0;
                msSiat.DrawIndexedSettings.PrimitiveCount = part.PrimitiveCount;

                aNode.RenderChildren();
            }

            private static void _OcclusionQueryAndDrawIndexed(RenderNode aNode, object aInstance)
            {
                OcclusionQuery query = (OcclusionQuery)aInstance;

                msActiveEffect.CommitChanges();
                query.Begin();
                msSiat.DrawIndexedPrimitives();
                query.End();

                aNode.RenderChildren();
            }

            private static void _PointLight(RenderNode aNode, object aInstance)
            {
                LightNode lightNode = (LightNode)aInstance;
                Light light = lightNode.Light;

                msActiveEffect[BuiltInParameters.siat_LightAttenuation].SetValue(light.LightAttenuation);
                msActiveEffect[BuiltInParameters.siat_LightDiffuse].SetValue(light.LightDiffuse);
                msActiveEffect[BuiltInParameters.siat_LightPositionOrDirection].SetValue(lightNode.WorldPosition);
                msActiveEffect[BuiltInParameters.siat_LightSpecular].SetValue(light.LightSpecular);

                aNode.RenderChildren();
            }

            private static void _InverseTransposeWorldTransform(RenderNode aNode, object aInstance)
            {
                Matrix3Wrapper inverseTransposeWorld = (Matrix3Wrapper)aInstance;
                msActiveEffect[BuiltInParameters.siat_InverseTransposeWorldTransform].SetValue(inverseTransposeWorld.Matrix.ToMatrix());

                aNode.RenderChildren();
            }

            private static void _PickColor(RenderNode aNode, object aInstance)
            {
                Color pickColor = (Color)aInstance;
                msActiveEffect[BuiltInParameters.siat_PickingColor].SetValue(pickColor.ToVector4());

                aNode.RenderChildren();
            }

            private static void _RenderTargetAndClear(RenderNode aNode, object aInstance)
            {
                RenderTargetPackage package = (RenderTargetPackage)aInstance;
                msGraphics.SetRenderTarget(package.Index, package.Target);
                msGraphics.DepthStencilBuffer = package.DSBuffer;
                msGraphics.Clear(ClearOptions.Target | ClearOptions.Stencil | ClearOptions.DepthBuffer, package.ClearColor, 1.0f, Siat.kDefaultReferenceStencil);

                aNode.RenderChildren();
            }

            private static void _ShadowRangeParameter(RenderNode aNode, object aInstance)
            {
                float range = (float)aInstance;
                msActiveEffect[BuiltInParameters.siat_ShadowRange].SetValue(range);

                aNode.RenderChildren();
            }

            private static void _SkinningTransforms(RenderNode aNode, object aInstance)
            {
                Vector4[] skinning = (Vector4[])aInstance;
                EffectParameter param = msActiveEffect[BuiltInParameters.siat_SkinningTransforms];

                param.SetValue(skinning);

                aNode.RenderChildren();
            }

            private static void _SpotLight(RenderNode aNode, object aInstance)
            {
                LightNode lightNode = (LightNode)aInstance;
                Light light = lightNode.Light;

                msActiveEffect[BuiltInParameters.siat_LightAttenuation].SetValue(light.LightAttenuation);
                msActiveEffect[BuiltInParameters.siat_LightDiffuse].SetValue(light.LightDiffuse);
                msActiveEffect[BuiltInParameters.siat_LightPositionOrDirection].SetValue(lightNode.WorldPosition);
                msActiveEffect[BuiltInParameters.siat_LightSpecular].SetValue(light.LightSpecular);
                msActiveEffect[BuiltInParameters.siat_SpotCutoffCosHalfAngle].SetValue(light.FalloffCosHalfAngle);
                msActiveEffect[BuiltInParameters.siat_SpotDirection].SetValue(lightNode.WorldLightDirection);
                msActiveEffect[BuiltInParameters.siat_SpotFalloffExponent].SetValue(light.FalloffExponent);

                aNode.RenderChildren();
            }

            private static void _SpotLightShadow(RenderNode aNode, object aInstance)
            {
                LightNode lightNode = (LightNode)aInstance;
                Light light = lightNode.Light;

                Texture2D texture = lightNode.ShadowRenderTarget.Target.GetTexture();

                msActiveEffect[BuiltInParameters.siat_LightAttenuation].SetValue(light.LightAttenuation);
                msActiveEffect[BuiltInParameters.siat_LightDiffuse].SetValue(light.LightDiffuse);
                msActiveEffect[BuiltInParameters.siat_LightPositionOrDirection].SetValue(lightNode.WorldPosition);
                msActiveEffect[BuiltInParameters.siat_LightSpecular].SetValue(light.LightSpecular);
                msActiveEffect[BuiltInParameters.siat_ShadowRange].SetValue(lightNode.Range);
                msActiveEffect[BuiltInParameters.siat_ShadowTexture].SetValue(texture);
                msActiveEffect[BuiltInParameters.siat_ShadowTransform].SetValue(lightNode.ShadowViewProjection * ShadowMaps.kShadowTransformPost);
                msActiveEffect[BuiltInParameters.siat_SpotDirection].SetValue(lightNode.WorldLightDirection);
                msActiveEffect[BuiltInParameters.siat_SpotCutoffCosHalfAngle].SetValue(light.FalloffCosHalfAngle);
                msActiveEffect[BuiltInParameters.siat_SpotFalloffExponent].SetValue(light.FalloffExponent);

                aNode.RenderChildren();
            }

            private static void _StandardEffectTransforms(RenderNode aNode, object aInstance)
            {
                msActiveEffect[BuiltInParameters.siat_InverseViewTransform].SetValue(Shared.InverseViewTransform);
                msActiveEffect[BuiltInParameters.siat_ViewTransform].SetValue(Shared.ViewTransform);
                msActiveEffect[BuiltInParameters.siat_ViewProjectionTransform].SetValue(Shared.ViewProjectionTransform);

                aNode.RenderChildren();
            }

            private static void _Nothing(RenderNode aNode, object aInstance)
            {
                aNode.RenderChildren();
            }

            private static void _StencilNoDeferred(RenderNode aNode, object aInstance)
            {
                msRenderState.ReferenceStencil = (int)StencilMasks.kNoDeferred;
                msRenderState.StencilDepthBufferFail = StencilOperation.Keep;
                msRenderState.StencilEnable = true;
                msRenderState.StencilFail = StencilOperation.Keep;
                msRenderState.StencilFunction = CompareFunction.Always;
                msRenderState.StencilPass = StencilOperation.Replace;
                aNode.RenderChildren();
                msRenderState.StencilEnable = false;
            }

            private static void _StencilDeferred(RenderNode aNode, object aInstance)
            {
                msRenderState.ReferenceStencil = Siat.kDefaultReferenceStencil;
                msRenderState.StencilDepthBufferFail = StencilOperation.Keep;
                msRenderState.StencilEnable = true;
                msRenderState.StencilFail = StencilOperation.Keep;
                msRenderState.StencilFunction = CompareFunction.Always;
                msRenderState.StencilPass = StencilOperation.Replace;
                aNode.RenderChildren();
                msRenderState.StencilEnable = false;
            }

            private static void _VertexDeclaration(RenderNode aNode, object aInstance)
            {
                VertexDeclaration declaration = (VertexDeclaration)aInstance;
                msGraphics.VertexDeclaration = declaration;

                aNode.RenderChildren();
            }

            private static void _ViewTransform(RenderNode aNode, object aInstance)
            {
                MatrixWrapper view = (MatrixWrapper)aInstance;
                msActiveEffect[BuiltInParameters.siat_ViewTransform].SetValue(view.Matrix);

                aNode.RenderChildren();
            }

            private static void _ViewProjectionTransform(RenderNode aNode, object aInstance)
            {
                MatrixWrapper viewProjection = (MatrixWrapper)aInstance;
                msActiveEffect[BuiltInParameters.siat_ViewProjectionTransform].SetValue(viewProjection.Matrix);

                aNode.RenderChildren();
            }

            private static void _WorldTransform(RenderNode aNode, object aInstance)
            {
                MatrixWrapper world = (MatrixWrapper)aInstance;
                msActiveEffect[BuiltInParameters.siat_WorldTransform].SetValue(world.Matrix);

                aNode.RenderChildren();
            }

            private static void _WorldTransformAndDrawIndexed(RenderNode aNode, object aInstance)
            {
                MatrixWrapper world = (MatrixWrapper)aInstance;
                msActiveEffect[BuiltInParameters.siat_WorldTransform].SetValue(world.Matrix);
                msActiveEffect.CommitChanges();
                msSiat.DrawIndexedPrimitives();

                aNode.RenderChildren();
            }
            #endregion

            public static RenderNodeDelegate DirectionalLight = _DirectionalLight;
            public static RenderNodeDelegate Effect = _Effect;
            public static RenderNodeDelegate EffectTechnique = _EffectTechnique;
            public static RenderNodeDelegate InverseTransposeWorldTransform = _InverseTransposeWorldTransform;
            public static RenderNodeDelegate Material = _Material;
            public static RenderNodeDelegate Mesh = _Mesh;
            public static RenderNodeDelegate Nothing = _Nothing;
            public static RenderNodeDelegate OcclusionQueryAndDrawIndexed = _OcclusionQueryAndDrawIndexed;
            public static RenderNodeDelegate PickColor = _PickColor;
            public static RenderNodeDelegate PointLight = _PointLight;
            public static RenderNodeDelegate RenderTargetAndClear = _RenderTargetAndClear;
            public static RenderNodeDelegate SetStandardEffectTransforms = _StandardEffectTransforms;
            public static RenderNodeDelegate ShadowRangeParameter = _ShadowRangeParameter;
            public static RenderNodeDelegate SkinningTransforms = _SkinningTransforms;
            public static RenderNodeDelegate SpotLight = _SpotLight;
            public static RenderNodeDelegate SpotLightShadow = _SpotLightShadow;
            public static RenderNodeDelegate StencilDeferred = _StencilDeferred;
            public static RenderNodeDelegate StencilNoDeferred = _StencilNoDeferred;
            public static RenderNodeDelegate VertexDeclaration = _VertexDeclaration;
            public static RenderNodeDelegate ViewProjectionTransform = _ViewProjectionTransform;
            public static RenderNodeDelegate ViewTransform = _ViewTransform;
            public static RenderNodeDelegate WorldTransform = _WorldTransform;
            public static RenderNodeDelegate WorldTransformAndDrawIndexed = _WorldTransformAndDrawIndexed;
        }

        public sealed class RenderTargetPackage : IDisposable
        {
            public RenderTargetPackage(int aIndex, RenderTarget2D aTarget, DepthStencilBuffer aDSBuffer, Color aClearColor)
            {
                Index = aIndex;
                Target = aTarget;
                DSBuffer = aDSBuffer;
                ClearColor = aClearColor;
            }

            public Color ClearColor;
            public readonly DepthStencilBuffer DSBuffer;
            public readonly int Index;
            public readonly RenderTarget2D Target;

            public void Dispose()
            {
                Target.Dispose();
            }
        }

    }
}
