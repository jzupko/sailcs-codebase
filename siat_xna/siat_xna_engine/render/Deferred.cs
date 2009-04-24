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

// #define DISABLE_MASKING
// #define DRAW_DEBUG
// #define DRAW_WIREFRAME

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

using siat.scene;

namespace siat.render
{
    public static class Deferred
    {
        public enum Shaders
        {
            kDirectional,
            kDirectionalMask,
            kPoint,
            kPointMask,
            kSpotlight,
            kSpotlightMask,
            kSpotlightShadow,
            kSpotlightShadowMask,
            kVertex
        }

        #region Shader source
        #region Common
        public const string kGlobals =
            @"
                static const float kLooseTolerance = 1e-3;

                static const float4x4 kProjAdjust = float4x4(0.5f, 0, 0, 0,
                                                             0,   -0.5f, 0, 0,
                                                             0,    0, 1, 0,
                                                             0.5f, 0.5f, 0, 1);

                static const int kShadowDimension = 512;
                static const float kShadowDelta = 1.0f / ((float)kShadowDimension);
                static const float kShadowDepthBias = 3.81e-4f;

                struct vsIn { float4 Position : POSITION; };
                struct vsOut
                {
                    float4 Position : POSITION;
                    float4 TextureLookup : TEXCOORD0;
                    float4 SmallTextureLookup : TEXCOORD1;
                };
            ";

        public enum kRegisters
        {
            LightAttenuation = 0,
            LightDiffuse = 1,
            LightSpecular = 2,
            LightV = 3,
            ShadowFarDepth = 4,
            ShadowTransform = 5,
            SpotDirection = 9,
            SpotFalloffCosAngle = 10,
            SpotFalloffExponent = 11,
            Range2 = 12
        }

        public const string kFragmentPre = 
            kGlobals +

            @"
                float3 LightAttenuation : register(c0);
                float3 LightDiffuse : register(c1);
                float3 LightSpecular : register(c2);
                float3 LightV : register(c3);
                float ShadowFarDepth : register(c4);
                float4x4 ShadowTransform : register(c5);
                float3 SpotDirection : register(c9);
                float SpotFalloffCosAngle : register(c10);
                float SpotFalloffExponent : register(c11);
                float Range2 : register(c12);

            	texture MrtTexture0 : register(t0);
                texture MrtTexture1 : register(t1);
                texture MrtTexture2 : register(t2);
                texture MrtTexture3 : register(t3);
                texture ShadowTexture : register(t4);

                sampler MrtSampler0 : register(s0) = sampler_state { texture = <MrtTexture0>; };
                sampler MrtSampler1 : register(s1)  = sampler_state { texture = <MrtTexture1>; };
                sampler MrtSampler2 : register(s2)  = sampler_state { texture = <MrtTexture2>; };
                sampler MrtSampler3 : register(s3)  = sampler_state { texture = <MrtTexture3>; };
                sampler ShadowSampler : register(s4)  = sampler_state { texture = <ShadowTexture>; };

                float4 Fragment(vsOut aIn) : COLOR
                {
                    float3 pixelDiffuse = tex2Dproj(MrtSampler0, aIn.TextureLookup).rgb;
                    float4 pixelSpecularShininess = tex2Dproj(MrtSampler1, aIn.TextureLookup).rgba;
                    float3 pixelEyePosition = tex2Dproj(MrtSampler2, aIn.TextureLookup).rgb;
                    float3 pixelEyeNormal = tex2Dproj(MrtSampler3, aIn.TextureLookup).rgb;
            ";

        public const string kMaskFragmentPre =
            kGlobals +

            @"
                float3 LightV : register(c3);
                float ShadowFarDepth : register(c4);
                float4x4 ShadowTransform : register(c5);
                float3 SpotDirection : register(c9);
                float SpotFalloffCosAngle : register(c10);
                float SpotFalloffExponent : register(c11);
                float Range2 : register(c12);

            	texture MrtTexture2 : register(t2);
                texture MrtTexture3 : register(t3);
                texture ShadowTexture : register(t4);

                sampler MrtSampler2 : register(s2) = sampler_state { texture = <MrtTexture2>; };
                sampler MrtSampler3 : register(s3) = sampler_state { texture = <MrtTexture3>; };
                sampler ShadowSampler : register(s4)  = sampler_state { texture = <ShadowTexture>; };

                float4 Fragment(vsOut aIn) : COLOR
                {
                    float3 pixelEyePosition = tex2Dproj(MrtSampler2, aIn.TextureLookup).rgb;
                    float3 pixelEyeNormal = tex2Dproj(MrtSampler3, aIn.TextureLookup).rgb;

            ";

        public const string kNonDirectionalPre =
            kFragmentPre +

            @"
                float3 lv = (LightV - pixelEyePosition);
                float distance = length(lv);
                lv = normalize(lv);
                
                float ndotl = dot(pixelEyeNormal, lv);

                float3 ev = normalize(-pixelEyePosition);
                float3 rv = (2.0f * ndotl * pixelEyeNormal) - lv;
			    
			    float att = 1.0f / (LightAttenuation.x + (LightAttenuation.y * distance) + (LightAttenuation.z * distance * distance));
                
                float idiff = max(ndotl, 0.0f);
                float ispec = (ndotl > 0.0f) ? pow(max(dot(ev, rv), 0.0f), max(pixelSpecularShininess.a, 1e-3)) : 0.0f;

                float3 ret = (LightDiffuse * pixelDiffuse * idiff)
                           + (LightSpecular * pixelSpecularShininess.rgb * ispec);

                ret *= att;
            ";

        public const string kSpotPre =
            kNonDirectionalPre +
            @"
                float spotDot = -dot(normalize(lv), SpotDirection);
		        float spot = pow(max(spotDot, 0.0f), max(SpotFalloffExponent, 1e-3));
		        if (spotDot < SpotFalloffCosAngle) { spot = 0.0f; }	

                ret *= spot;
            ";
        #endregion

        public static readonly string[] kSources = new string[]
            {
                // Directional
                kFragmentPre +
                @"
                    float3 lv = normalize(-LightV);
                    float ndotl = dot(pixelEyeNormal, lv);

                    float3 ev = normalize(-pixelEyePosition);
                    float3 rv = (2.0f * ndotl * pixelEyeNormal) - lv;

                    float idiff = max(ndotl, 0.0f);
                    float ispec = (ndotl > 0.0f) ? pow(max(dot(ev, rv), 0.0f), max(pixelSpecularShininess.a, 1e-3)) : 0.0f;

                    float3 ret = (LightDiffuse * pixelDiffuse * idiff)
                               + (LightSpecular * pixelSpecularShininess.rgb * ispec);
                "
#if DRAW_DEBUG
#if DISABLE_MASKING
                + "return float4(0, 1, 0, 1); }",
#else
                + "return float4(0, ndotl, 0, 1); }",
#endif
#else
                + "return float4(ret, 1); }",
#endif

                // Directional mask
                kMaskFragmentPre +
                @"
                    float3 lv = -LightV;
                    float ndotl = dot(pixelEyeNormal, lv);

                    if (ndotl < kLooseTolerance) { discard; }

                    return float4(0, 0, 0, 1);
                    }
                ",

                // Point
                kNonDirectionalPre
#if DRAW_DEBUG
#if DISABLE_MASKING
                + "return float4(0, 0, 1, 1); }",
#else
                + "return float4(0, 0, ndotl, 1); }",
#endif
#else
                + "return float4(ret, 1); }",
#endif
                // Point mask
                kMaskFragmentPre +
                @"
                    float3 lv = (LightV - pixelEyePosition);
                    float ndotl = dot(pixelEyeNormal, lv);
                    float d2 = dot(lv, lv);

                    if (d2 > Range2 || ndotl < kLooseTolerance) { discard; }

                    return float4(0, 0, 0, 1);
                    }
                ",

                // Spot
#if DISABLE_MASKING
                kSpotPre +
#else
                kNonDirectionalPre +
#endif
                @"
                    return float4(ret, 1);
                    }
                ",

                // Spot mask
                kMaskFragmentPre +
                @"
                    float3 lv = (LightV - pixelEyePosition);
                    float ndotl = dot(pixelEyeNormal, lv);
			        
                    float spotDot = -dot(normalize(lv), SpotDirection);
			        float spot = pow(max(spotDot, 0.0f), max(SpotFalloffExponent, 1e-3));
			        if (spotDot < SpotFalloffCosAngle) { spot = 0.0f; }	

                    float d2 = dot(lv, lv);

                    if (d2 > Range2 || ndotl < kLooseTolerance || spot < kLooseTolerance) { discard; }

                    return float4(0, 0, 0, spot);
                    }
                ",

                // Spot shadow 
#if DISABLE_MASKING
                kSpotPre
                +
                @"
                    float pixelDepth = saturate((distance / ShadowFarDepth) - kShadowDepthBias);
                    float4 shadowTexCoords = mul(float4(pixelEyePosition, 1.0f), ShadowTransform);

		            float shadowDepth = tex2Dproj(ShadowSampler, shadowTexCoords).x;
    	            if (pixelDepth > shadowDepth) { ret = float3(0, 0, 0); }
                "
#else
                kNonDirectionalPre
#endif

#if DRAW_DEBUG
#if DISABLE_MASKING
                + "return float4(1, 0, 0, 1); }",
#else
                + "return float4(ndotl, 0, 0, 1); }",
#endif

#else
                + "return float4(ret, 1); }",
#endif

                // Spot shadow mask
                kMaskFragmentPre +
                @"
                    float3 lv = (LightV - pixelEyePosition);
                    float ndotl = dot(pixelEyeNormal, lv);

                    float spotDot = -dot(normalize(lv), SpotDirection);
			        float spot = pow(max(spotDot, 0.0f), max(SpotFalloffExponent, 1e-3));
			        if (spotDot < SpotFalloffCosAngle) { spot = 0.0f; }	

                    float d2 = dot(lv, lv);

                    float distance = length(lv);
                    float pixelDepth = ((distance / ShadowFarDepth) - kShadowDepthBias);
                    float4 shadowTexCoords = mul(float4(pixelEyePosition, 1.0f), ShadowTransform);
    
                    float offset = (kShadowDelta * shadowTexCoords.w);
                    float noffset = -offset;

                    float4 shadowDepths;
                    shadowDepths.x = tex2Dproj(ShadowSampler, shadowTexCoords + float4(noffset, noffset, 0, 0)).x;
                    shadowDepths.y = tex2Dproj(ShadowSampler, shadowTexCoords + float4( offset, noffset, 0, 0)).x;
                    shadowDepths.z = tex2Dproj(ShadowSampler, shadowTexCoords + float4(noffset,  offset, 0, 0)).x;
                    shadowDepths.w = tex2Dproj(ShadowSampler, shadowTexCoords + float4( offset,  offset, 0, 0)).x;

                    float4 c = (pixelDepth <= shadowDepths);
                    float factor = (c.x + c.y + c.z + c.w) * 0.25;

                    if (factor < kLooseTolerance
                        || d2 > Range2 
                        || ndotl < kLooseTolerance
                        || spot < kLooseTolerance
                    )
                    {
                        discard;
                    }

                    return float4(0, 0, 0, factor * spot);
                    }
                ",

                // Vertex
                kGlobals +
                @"
                    float4x4 WorldViewProjectionTransform : register(c0);
                    float4 ProjFactors : register(c4);

                    vsOut Vertex(vsIn aIn)
                    {
                        vsOut ret;

                        ret.Position = mul(aIn.Position, WorldViewProjectionTransform);

                        float t = (0.5f * ret.Position.w);

                        ret.TextureLookup = float4(
                            (ret.Position.x * 0.5f) + (t * ProjFactors.x),
                           -(ret.Position.y * 0.5f) + (t * ProjFactors.y),
                            ret.Position.z,
                            ret.Position.w); 
                        
                        ret.SmallTextureLookup = float4(
                            (ret.Position.x * 0.5f) + (t * ProjFactors.z),
                           -(ret.Position.y * 0.5f) + (t * ProjFactors.w),
                            ret.Position.z,
                            ret.Position.w); 

                        return ret;
                    }
                "
            };
        #endregion

        public const int kCount = 4;
        public static readonly SurfaceFormat[] kFormats = new SurfaceFormat[]
            { SurfaceFormat.HalfVector4, // RGB: diffuse-color
              SurfaceFormat.HalfVector4, // RGB: specular-color, A: shininess
              SurfaceFormat.HalfVector4, // RGB: eye-position
              SurfaceFormat.HalfVector4 }; // RGB: eye-normal

        #region Private members
        private static readonly CompiledShader[] msShadersC = new CompiledShader[kSources.Length];
        private static PixelShader[] msPixelShaders = new PixelShader[kSources.Length - 1];
        internal static VertexShader msVertexShader = null;

        static Deferred()
        {
            int count = kSources.Length - 1;
            for (int i = 0; i < count; i++)
            {
                msShadersC[i] = ShaderCompiler.CompileFromSource(kSources[i], null, null, CompilerOptions.None, "Fragment", ShaderProfile.PS_2_0, TargetPlatform.Windows);
            }

            msShadersC[count] = ShaderCompiler.CompileFromSource(kSources[count], null, null, CompilerOptions.None, "Vertex", ShaderProfile.VS_2_0, TargetPlatform.Windows);
        }

        private static bool msbActive = false;
        private static bool msbLoaded = false;
        private static RenderTarget2D[] msTargets = new RenderTarget2D[kCount];
        
        private static int msOldQuality = 0;
        private static MultiSampleType msOldType = MultiSampleType.NonMaskable;

        private static void _CommonLight()
        {
            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            for (int i = 0; i < kCount; i++) { gd.Textures[i] = msTargets[i].GetTexture(); }
            _CommonStates();
            gd.VertexDeclaration = siat.UnitSphereMeshPart.VertexDeclaration;
            gd.VertexShader = msVertexShader;
        }

        private static void _CommonStates()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            RenderState rs = gd.RenderState;

            rs.AlphaTestEnable = false;
            rs.CullMode = Utilities.kFrontFaceCulling;
            rs.DepthBufferEnable = false;
            rs.DepthBufferWriteEnable = false;
            rs.DestinationBlend = Blend.One;
#if DRAW_WIREFRAME
            rs.FillMode = FillMode.WireFrame;
#else
            rs.FillMode = FillMode.Solid;
#endif

#if DISABLE_MASKING
            rs.SourceBlend = Blend.One;
#else
            rs.SourceBlend = Blend.DestinationAlpha;
#endif
            rs.ReferenceStencil = Siat.kDefaultStencilMask;
            rs.StencilDepthBufferFail = StencilOperation.Keep;
            rs.StencilEnable = true;
            rs.StencilFail = StencilOperation.Keep;

            for (int i = 0; i < kCount + 1; i++)
            {
                gd.SamplerStates[i].AddressU = TextureAddressMode.Clamp;
                gd.SamplerStates[i].AddressV = TextureAddressMode.Clamp;
                gd.SamplerStates[i].MagFilter = TextureFilter.Point;
                gd.SamplerStates[i].MinFilter = TextureFilter.Point;
                gd.SamplerStates[i].MipFilter = TextureFilter.None;
            }
        }

        private static void _CommonLightStates()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            RenderState rs = gd.RenderState;

            rs.AlphaBlendEnable = true;
            rs.ColorWriteChannels = ColorWriteChannels.All;
#if DISABLE_MASKING
            rs.StencilFunction = CompareFunction.NotEqual;
            rs.StencilMask = (int)RenderRoot.StencilMasks.kNoDeferred;
            rs.StencilPass = StencilOperation.Keep;
            rs.StencilWriteMask = Siat.kDefaultStencilMask;
#else
            rs.StencilFunction = CompareFunction.Equal;
            rs.StencilMask = (int)RenderRoot.StencilMasks.kDeferredMask;
            rs.StencilPass = StencilOperation.Zero;
            rs.StencilWriteMask = (int)RenderRoot.StencilMasks.kDeferredMask;
#endif
        }

        private static void _CommonMaskStates()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            RenderState rs = gd.RenderState;

            rs.AlphaBlendEnable = false;
            rs.ColorWriteChannels = ColorWriteChannels.Alpha;
            rs.StencilFunction = CompareFunction.NotEqual;
            rs.StencilMask = (int)RenderRoot.StencilMasks.kNoDeferred;
            rs.StencilPass = StencilOperation.Replace;
            rs.StencilWriteMask = (int)RenderRoot.StencilMasks.kDeferredMask;
        }

        private static void _Light(LightNode aNode, int aReferenceStencil)
        {
            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            bool bShadows = aNode.bCastShadow;

            Matrix scale;
            if (aNode.Light.Type == LightType.Spot)
            {
                float t = (float)Math.Tan(0.5f * aNode.Light.FalloffAngleInRadians) * aNode.Range;
                scale = Matrix.CreateScale(t, t, aNode.Range);
            }
            else
            {
                scale = Matrix.CreateScale(aNode.Range);
            }
            Matrix wvp = scale * aNode.WorldTransform * Shared.InfiniteViewProjectionTransform;
            gd.SetVertexShaderConstant(0, Matrix.Transpose(wvp));
            gd.SetVertexShaderConstant(4, new Vector2(
                1.0f + (float)(1.0 / gd.PresentationParameters.BackBufferWidth),
                1.0f + (float)(1.0 / gd.PresentationParameters.BackBufferHeight)));

            gd.SetPixelShaderConstant((int)kRegisters.LightAttenuation, aNode.Light.LightAttenuation);
            gd.SetPixelShaderConstant((int)kRegisters.LightDiffuse, aNode.Light.LightDiffuse);
            gd.SetPixelShaderConstant((int)kRegisters.LightSpecular, aNode.Light.LightSpecular);
            gd.SetPixelShaderConstant((int)kRegisters.LightV, (aNode.Light.Type == LightType.Directional) ? Vector3.TransformNormal(aNode.WorldLightDirection, Shared.ViewTransform) : Vector3.Transform(aNode.WorldPosition, Shared.ViewTransform));
            gd.SetPixelShaderConstant((int)kRegisters.Range2, new Vector4(aNode.Range * aNode.Range));
            if (bShadows)
            {
                Texture2D tex = aNode.ShadowRenderTarget.Target.GetTexture();
                gd.Textures[kCount] = tex;
                gd.SetPixelShaderConstant((int)kRegisters.ShadowFarDepth, new Vector4(aNode.Range));

                Matrix m = Matrix.Transpose(Shared.InverseViewTransform * aNode.ShadowViewProjection * ShadowMaps.kShadowTransformPost);

                gd.SetPixelShaderConstant((int)kRegisters.ShadowTransform, m);
            }
            if (aNode.Light.Type == LightType.Spot)
            {
                gd.SetPixelShaderConstant((int)kRegisters.SpotDirection, Vector3.TransformNormal(aNode.WorldLightDirection, Shared.ViewTransform));
                gd.SetPixelShaderConstant((int)kRegisters.SpotFalloffCosAngle, new Vector4(aNode.Light.FalloffCosHalfAngle));
                gd.SetPixelShaderConstant((int)kRegisters.SpotFalloffExponent, new Vector4(aNode.Light.FalloffExponent));
            }

            MeshPart part = (aNode.Light.Type == LightType.Spot) ? siat.UnitFrustumMeshPart : siat.UnitSphereMeshPart;

            gd.Indices = part.Indices;
            gd.Vertices[0].SetSource(part.Vertices, 0, part.VertexStride);
            siat.DrawIndexedSettings.PrimitiveType = part.PrimitiveType;
            siat.DrawIndexedSettings.BaseVertex = 0;
            siat.DrawIndexedSettings.MinVertexIndex = 0;
            siat.DrawIndexedSettings.NumberOfVertices = part.VertexCount;
            siat.DrawIndexedSettings.StartIndex = 0;
            siat.DrawIndexedSettings.PrimitiveCount = part.PrimitiveCount;

            #region Masking pass
#if !DISABLE_MASKING
            _CommonMaskStates();
            if (aNode.Light.Type == LightType.Directional) { gd.PixelShader = msPixelShaders[(int)Shaders.kDirectionalMask]; }
            else if (aNode.Light.Type == LightType.Point) { gd.PixelShader = msPixelShaders[(int)Shaders.kPointMask]; }
            else if (aNode.Light.Type == LightType.Spot) 
            {
                if (bShadows) { gd.PixelShader = msPixelShaders[(int)Shaders.kSpotlightShadowMask]; }
                else { gd.PixelShader = msPixelShaders[(int)Shaders.kSpotlightMask]; }
            }
            siat.DrawIndexedPrimitives();
#endif
            #endregion

            #region Light pass
            _CommonLightStates();
            if (aNode.Light.Type == LightType.Directional) { gd.PixelShader = msPixelShaders[(int)Shaders.kDirectional]; }
            else if (aNode.Light.Type == LightType.Point) { gd.PixelShader = msPixelShaders[(int)Shaders.kPoint]; }
            else if (aNode.Light.Type == LightType.Spot)
            {
                if (bShadows) { gd.PixelShader = msPixelShaders[(int)Shaders.kSpotlightShadow]; }
                else { gd.PixelShader = msPixelShaders[(int)Shaders.kSpotlight]; }
            }
            siat.DrawIndexedPrimitives();
            #endregion
        }
        #endregion

        public static bool bActive { get { return msbActive; } }

        public static void Activate()
        {
            if (!msbActive)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice gd = siat.GraphicsDevice;
                RenderRoot._ResetTrees();

                msOldQuality = gd.PresentationParameters.MultiSampleQuality;
                msOldType = gd.PresentationParameters.MultiSampleType;
                
                PresentationParameters pms = gd.PresentationParameters;
                pms.MultiSampleQuality = 0;
                pms.MultiSampleType = MultiSampleType.None;
                gd.Reset(pms);

                gd.DepthStencilBuffer = new DepthStencilBuffer(gd, gd.PresentationParameters.BackBufferWidth, gd.PresentationParameters.BackBufferHeight, gd.DepthStencilBuffer.Format, MultiSampleType.None, 0);

                msbActive = true;

                OnLoad();

                Cell.RefreshAll();
            }
        }

        public static void Deactivate()
        {
            if (msbActive)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice gd = siat.GraphicsDevice;
                RenderRoot._ResetTrees();

                OnUnload();

                PresentationParameters pms = gd.PresentationParameters;
                pms.MultiSampleQuality = msOldQuality;
                pms.MultiSampleType = msOldType;
                gd.Reset(pms);

                gd.DepthStencilBuffer = new DepthStencilBuffer(gd, gd.PresentationParameters.BackBufferWidth, gd.PresentationParameters.BackBufferHeight, gd.DepthStencilBuffer.Format, msOldType, msOldQuality);

                msbActive = false;

                Cell.RefreshAll();
            }
        }

        public static void OnLoad()
        {
            if (msbActive && !msbLoaded)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice gd = siat.GraphicsDevice;

                #region Render targets
                int width = gd.PresentationParameters.BackBufferWidth;
                int height = gd.PresentationParameters.BackBufferHeight;

                for (int i = 0; i < kCount; i++)
                {
                    msTargets[i] = new RenderTarget2D(gd, width, height, 1, kFormats[i], RenderTargetUsage.PlatformContents);
                }
                #endregion

                #region Shaders
                int count = msShadersC.Length - 1;
                for (int i = 0; i < count; i++)
                {
                    msPixelShaders[i] = new PixelShader(gd, msShadersC[i].GetShaderCode());
                }
                msVertexShader = new VertexShader(gd, msShadersC[count].GetShaderCode());
                #endregion

                msbLoaded = true;

                DeferredPost.OnLoad();
            }
        }

        public static void OnResize()
        {
            OnUnload();
            OnLoad();
            DeferredPost.OnResize();
        }

        public static void OnUnload()
        {
            if (msbLoaded)
            {
                DeferredPost.OnUnload();

                #region Shaders
                msVertexShader.Dispose(); msVertexShader = null;
                int count = msPixelShaders.Length;
                for (int i = 0; i < count; i++)
                {
                    msPixelShaders[i].Dispose(); msPixelShaders[i] = null;
                }
                #endregion

                #region Render targets
                for (int i = kCount - 1; i >= 0; i--) { msTargets[i].Dispose(); msTargets[i] = null; }
                #endregion

                msbLoaded = false;
            }
        }

        public static void SetTargetsAsTextures()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;

            for (int i = 0; i < kCount; i++) { gd.Textures[i] = msTargets[i].GetTexture(); }
        }

        public static void UnsetTexturesOfTargets()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;

            for (int i = 0; i < kCount; i++) { gd.Textures[i] = null; }
        }

        public static void SetRenderTargets()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;

            for (int i = 0; i < kCount; i++) { gd.SetRenderTarget(i, msTargets[i]); }
            gd.RenderState.ColorWriteChannels2 = ColorWriteChannels.None;
            gd.RenderState.ColorWriteChannels3 = ColorWriteChannels.None;
            gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil | ClearOptions.Target, Color.Black, 1.0f, (int)RenderRoot.StencilMasks.kNoDeferred);
            gd.RenderState.ColorWriteChannels2 = ColorWriteChannels.All;
            gd.RenderState.ColorWriteChannels3 = ColorWriteChannels.All;
            gd.Clear(ClearOptions.Target, Color.Blue, 1.0f, Siat.kDefaultReferenceStencil);
        }

        public static void RenderLights(List<LightNode> aLights)
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            RenderState rs = gd.RenderState;

            _CommonLight();
            int count = aLights.Count;
            for (int i = 0; i < count; i++) { _Light(aLights[i], i+1); }
            for (int i = 0; i < kCount + 1; i++) { gd.Textures[i] = null; }

            rs.DepthBufferEnable = true;
            rs.StencilEnable = false;
            rs.StencilMask = Siat.kDefaultStencilMask;
            rs.StencilWriteMask = Siat.kDefaultStencilMask;
        }
    }
}
