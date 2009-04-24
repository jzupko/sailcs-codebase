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

// #define SHOW_AO
// #define SHOW_EDGE

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using siat.scene;

namespace siat.render
{
    public static class DeferredPost
    {
        public const SurfaceFormat kFormat = SurfaceFormat.HalfVector4;
        public const int kSmallTargetSize = 256;

        #region Shader source
        #region Declarations
        public enum kShaders
        {
            kToLDR = 0,
            kMb = 1,
            kDofH = 2,
            kDofV = 3,
            kAntiAliasing = 4,
            kBloomMerge = 5,
            kBloomH = 6,
            kBloomV = 7,
            kBloomHP = 8,
            kFog = 9,
            kAoBlur = 10,
            kAo = 11
        }

        public static readonly ShaderProfile[] kShaderProfiles = new ShaderProfile[]
            {
                ShaderProfile.PS_2_0, // ToLDR
                ShaderProfile.PS_2_0, // Mb
                ShaderProfile.PS_2_0, // DofH
                ShaderProfile.PS_2_0, // DofV
                ShaderProfile.PS_3_0, // AntiAliasing
                ShaderProfile.PS_2_0, // BloomMerge
                ShaderProfile.PS_2_0, // BloomH
                ShaderProfile.PS_2_0, // BloomV
                ShaderProfile.PS_2_0, // BloomHP
                ShaderProfile.PS_2_0, // Fog
                ShaderProfile.PS_3_0, // AoBlur
                ShaderProfile.PS_3_0 // Ao
            };

        public static readonly bool[] kFullTargets = new bool[]
            {
                true, // ToLDR
                true, // Mb
                true, // DofH
                true, // DofV
                true, // AntiAliasing
                true, // Bloom Merge
                false, // BloomH
                false, // BloomV
                false, // BloomHP
                true, // Fog
                true, // AoBlur
                true // Ao
            };

        public enum kGeneralConstants
        {
            kJitterDelta = 0,
            kNearFarFocalLength = 1,
            kInverseViewC0 = 2,
            kInverseViewC1 = 3,
            kInverseViewC2 = 4,
            kInverseViewC3 = 5,
            kProjectionC0 = 6,
            kProjectionC1 = 7,
            kProjectionC2 = 8,
            kProjectionC3 = 9,
            kInverseViewPrevViewProjectionC0 = 10,
            kInverseViewPrevViewProjectionC1 = 11,
            kInverseViewPrevViewProjectionC2 = 12,
            kInverseViewPrevViewProjectionC3 = 13,
            kResolution = 14,
            kInverseResolution = 15,
            kGamma = 16
        }

        public static readonly int kFirstCustomConstant = Enum.GetNames(typeof(kGeneralConstants)).Length;

        public enum kLDRConstants { }

        public enum kMbConstants { }

        public enum kDofHConstants { }
        public enum kDofVConstants
        {
            kFocalPlane = 0,
            kHyperfocalDistance = 1
        }

        public enum kAntiAliasingConstants { }

        public enum kBloomMergeConstants { }
        public enum kBloomHConstants { }
        public enum kBloomVConstants { }
        public enum kBloomHPConstants { }

        public enum kFogConstants
        {
            kColor = 0,
            kFalloffHeightDensity = 1
        }

        public enum kAoBlurConstants { }
        public enum kAoConstants { }

        public static readonly int[] kConstantCounts = new int[]
            {
                Enum.GetNames(typeof(kLDRConstants)).Length,
                Enum.GetNames(typeof(kMbConstants)).Length,
                Enum.GetNames(typeof(kDofHConstants)).Length,
                Enum.GetNames(typeof(kDofVConstants)).Length,
                Enum.GetNames(typeof(kAntiAliasingConstants)).Length,
                Enum.GetNames(typeof(kBloomMergeConstants)).Length,
                Enum.GetNames(typeof(kBloomHConstants)).Length,
                Enum.GetNames(typeof(kBloomVConstants)).Length,
                Enum.GetNames(typeof(kBloomHPConstants)).Length,
                Enum.GetNames(typeof(kFogConstants)).Length,
                Enum.GetNames(typeof(kAoBlurConstants)).Length,
                Enum.GetNames(typeof(kAoConstants)).Length,
            };
        #endregion

        #region Pre
        public const string kFragmentPrePre =
            Deferred.kGlobals +
            @"
                static const float kPi = 3.141592653589;
                static const float kInvPi = 1.0 / kPi;

                float4 JitterDelta : register(c0);
                float4 NearFarFocalLength : register(c1);
                float4x4 InverseView : register(c2);
                float4x4 Projection : register(c6);
                float4x4 InverseViewPrevViewProjection : register(c10);
                float2 Resolution : register(c14);
                float2 InverseResolution : register(c15);
                float Gamma : register(c16);

        	    texture MrtTexture0 : register(t0);
                texture MrtTexture1 : register(t1);
                texture MrtTexture2 : register(t2);
                texture MrtTexture3 : register(t3);
                texture FinalTexture : register(t4);
                texture SmallFinalTexture : register(t5);
                texture NoiseTexture : register(t6);

                sampler MrtSampler0 : register(s0) = sampler_state { texture = <MrtTexture0>; };
                sampler MrtSampler1 : register(s1) = sampler_state { texture = <MrtTexture1>; };
                sampler MrtSampler2 : register(s2) = sampler_state { texture = <MrtTexture2>; };
                sampler MrtSampler3 : register(s3) = sampler_state { texture = <MrtTexture3>; };
                sampler FinalSampler : register(s4) = sampler_state { texture = <FinalTexture>; };
                sampler SmallFinalSampler : register(s5) = sampler_state { texture = <SmallFinalTexture>; };
                sampler NoiseSampler : register(s6) = sampler_state { texture = <NoiseTexture>; };
            ";

        public const string kFragmentPrePost =
            @"
                float4 Fragment(vsOut aIn) : COLOR
                {
                    float3 pixelDiffuse = tex2Dproj(MrtSampler0, aIn.TextureLookup).rgb;
                    float4 pixelSpecularShininess = tex2Dproj(MrtSampler1, aIn.TextureLookup).rgba;
                    float3 pixelEyePosition = tex2Dproj(MrtSampler2, aIn.TextureLookup).rgb;
                    float3 pixelEyeNormal = tex2Dproj(MrtSampler3, aIn.TextureLookup).rgb;
                    float3 pixelFinalColor = tex2Dproj(FinalSampler, aIn.TextureLookup).rgb;
                    float3 pixelFinalColorSmall = tex2Dproj(SmallFinalSampler, aIn.SmallTextureLookup).rgb;
            ";

        public const string kFragmentPre =
            kFragmentPrePre +
            kFragmentPrePost;

        #endregion

        #region Anti-aliasing
        /// <summary>
        /// Anti-aliasing for deferred shading
        /// </summary>
        /// <remarks>
        /// From: Koonce, R. 2008. "Deferred Shading in Tabula Rasa", GPU Gems 3, 
        ///     Addison-Wesley, ISBN: 0-321-51526-9
        /// </remarks>
        public const string kEdge =
            @"
                static const float kTaps = 8.0;
                static const float kInvTaps = 1.0 / kTaps;

                static const float kHalfTaps = 4.0;
                static const float kInvHalfTaps = 1.0 / kHalfTaps;

                static const float kInvBlendFactor = kInvTaps;

                static const float kNormalFactor = 0.3f;

                static const float2 kOffsets[kTaps] = 
                    {
                        float2(-1, -1),
                        float2( 0, -1),
                        float2( 1, -1),
                        float2( 1,  0),
                        float2( 1,  1),
                        float2( 0,  1),
                        float2(-1,  1),
                        float2(-1,  0)
                    };
            " +
            kFragmentPre +
            @"
                float4 uvs[kTaps];

                float factor = 0.0;
                for (int i = 0; i < kHalfTaps; i++)
                {
                    int i0 = i;
                    int i1 = i + kHalfTaps;

                    uvs[i0] = aIn.TextureLookup + float4(JitterDelta.xy * aIn.TextureLookup.w * kOffsets[i0], 0, 0);
                    uvs[i1] = aIn.TextureLookup + float4(JitterDelta.xy * aIn.TextureLookup.w * kOffsets[i1], 0, 0);

                    float3 normal0 = tex2Dproj(MrtSampler3, uvs[i0]).xyz;
                    float3 normal1 = tex2Dproj(MrtSampler3, uvs[i1]).xyz;

                    factor += step(kNormalFactor, abs(dot(normal1, pixelEyeNormal) - dot(normal0, pixelEyeNormal)));
                }
                factor *= kInvHalfTaps;

                float3 ret = float3(0, 0, 0);
                for (int i = 0; i < kTaps; i++)
                {
                    ret += lerp(pixelFinalColor, tex2Dproj(FinalSampler, uvs[i]).rgb, factor);
                }
                ret *= kInvBlendFactor;
            " +
#if !SHOW_EDGE
            "return float4(ret, 1); }";
#else
            "return float4(factor, factor, factor, 1); }";
#endif
        #endregion

        #region Motion blur
        public static readonly string kMb =
            @"
                static const float kTaps = 5;
                static const float kInverseTaps = 1.0 / kTaps;

             " +
            kFragmentPre +
            @"
                float4 cur = mul(float4(pixelEyePosition, 1), Projection);
                cur /= cur.w;
                float4 pre = mul(float4(pixelEyePosition, 1), InverseViewPrevViewProjection);
                pre /= pre.w;
                
                float3 color = pixelFinalColor;
                float2 vel = (float2(cur.x, cur.y) - float2(pre.x, pre.y)) * 0.25;

                if (dot(vel, vel) > (kLooseTolerance * kLooseTolerance))
                {
                    float4 maxoffset = float4((aIn.TextureLookup.w * JitterDelta.xy), 0, 0);
                    float4 offset = float4((aIn.TextureLookup.w * vel), 0, 0) * kInverseTaps;
                    offset = min(offset, maxoffset);

                    float4 uv = (aIn.TextureLookup + offset);
                    for (int i = 1; i < kTaps; ++i, uv += offset)
                    {
                        color += tex2Dproj(FinalSampler, uv).rgb;
                    }
                    color *= kInverseTaps;
                }

                return float4(color, 1);
                }
            ";
        #endregion

        #region Depth of field
        public static readonly string kDofPre =
            @"
                static const float kWidth = 5;
                static const float kFactor = (1.0 / ((2.0 * kWidth) + 1.0));
                static const float kMaxF2 = (3.0 / 4.0);
            " +

            "   float4 FocalPlane : register(c" + kFirstCustomConstant.ToString() + ");" +
            "   float HyperfocalDistance : register(c" + (kFirstCustomConstant + 1).ToString() + ");" +
            kFragmentPre +
            @"
                float2 offset = (JitterDelta.xy * aIn.TextureLookup.w);
                float3 accum = float3(0, 0, 0);

                for (float i = -kWidth; i <= kWidth; i += 1.0)
                {
            ";

        public const string kDofHLoop =
            "accum += tex2Dproj(FinalSampler, aIn.TextureLookup + float4(offset.x * i, 0, 0, 0)).rgb;";

        public const string kDofVLoop =
            "accum += tex2Dproj(FinalSampler, aIn.TextureLookup + float4(0, offset.y * i, 0, 0)).rgb;";

        public const string kDofPost =
            @"
                }

                float f = dot(float4(pixelEyePosition, 1.0), FocalPlane);
                float f2 = min((f * f), kMaxF2);

                float3 ret = lerp(pixelFinalColor.rgb, (accum * kFactor), f2);

                return float4(ret.rgb, 1);
                }
            ";
        #endregion

        #region Common
        public const string kGammaCorrection =
            kFragmentPre +
             @"
                    return float4(pow(pixelFinalColor, (1.0 / Gamma)), 1);
                }
            ";

        public const string kPassThrough =
            kFragmentPre +
            @"
                    return float4(pixelFinalColor, 1);
                }
            ";

        public const string kBlendSmallFull =
            kFragmentPre +
            @"
                return float4(lerp(pixelFinalColor, pixelFinalColorSmall, 0.5), 1);
                }
            ";
        #endregion

        #region Bloom
        public const string kBloomPre =
            @"
                static const float kGaussKernelRadius = 5.0;
                static const float kGaussKernelWeights[(kGaussKernelRadius * 2) + 1] =
                    { 
                        0.0621, 0.1024, 0.1511, 0.1995, 0.2357, 
                        0.2491,
                        0.2357, 0.1995, 0.1511, 0.1024, 0.0621,
                    };
                static const float kGaussKernelStep = 1.0;
                static const float kGaussKernelMinStep = 0.01;
            " +
            kFragmentPre +
            @"
                float2 offset = kGaussKernelStep * (JitterDelta.zw * aIn.SmallTextureLookup.w);
                float3 accum = float3(0, 0, 0);

                for (float i = -kGaussKernelRadius; i <= kGaussKernelRadius; i += kGaussKernelStep)
                {
            ";

        public const string kBloomHLoop =
            "accum += kGaussKernelWeights[i + kGaussKernelRadius] * tex2Dproj(SmallFinalSampler, aIn.SmallTextureLookup + float4(offset.x * i, 0, 0, 0)).rgb;";

        public const string kBloomVLoop =
            "accum += kGaussKernelWeights[i + kGaussKernelRadius] * tex2Dproj(SmallFinalSampler, aIn.SmallTextureLookup + float4(0, offset.y * i, 0, 0)).rgb;";

        public const string kBloomPost =
            @"
                }

                float3 ret = accum;

                return float4(ret, 1);
                }
            ";

        public const string kBloomHP =
            @"
                static const float kHighPass = 1.0f;
                static const float3 kLowClamp = float3(0, 0, 0);
                static const float3 kAdjust = float3(kHighPass, kHighPass, kHighPass);
            " +
            kFragmentPre +
            @"
                float3 ret = max(pixelFinalColor - kAdjust, kLowClamp);

                return float4(ret, 1);
                }
            ";

        public const string kBloomMerge =
            kFragmentPre +
            @"
                return float4(pixelFinalColor + pixelFinalColorSmall, 1);
                }
            ";
        #endregion

        #region Fog
        public static readonly string kFog =
                "   static const float kFactor = 4.0f;" +
                "   static const float kInvFactor = (1.0 / kFactor);" +
                "   float3 FogColor : register(c" + kFirstCustomConstant.ToString() + ");" +
                "   float3 FogFalloffHeightDensity : register(c" + (kFirstCustomConstant + 1).ToString() + ");" +
                kFragmentPre +
                @"
                        float falloff = (1.0 - FogFalloffHeightDensity.x);
                        float height = FogFalloffHeightDensity.y;
                        float dens = FogFalloffHeightDensity.z;

                        float3 world = mul(float4(pixelEyePosition, 1), InverseView).xyz;

                        float t = dens * (1.0 - exp(falloff * kFactor * pixelEyePosition.z));
                        float c = (world.y - height);
                        if (c > 0.0) { t *= exp(-c * kInvFactor); }

                        float3 ret = lerp(pixelFinalColor, FogColor, t);

                        return float4(ret, 1);
                    }
                ";
        #endregion

        #region Ambient occlusion
        public const string kAoBlur =
            @"
                static const float kStart = -2;
                static const float kStop = 2;
                static const float kFactor = (1.0 / ((kStop - kStart + 1) * (kStop - kStart + 1)));
            " +
            kFragmentPre +
            @"
                float accum = 0.0f;

                for (int i = kStart; i <= kStop; i++)
                {
                    for (int j = kStart; j <= kStop; j++)
                    {
                        float4 uv = aIn.TextureLookup + float4(
                            JitterDelta.x * aIn.TextureLookup.w * i,
                            JitterDelta.y * aIn.TextureLookup.w * j,
                            0, 0);

                        accum += tex2Dproj(FinalSampler, uv).a;
                    }
                }

                accum *= kFactor;
            " +
#if !SHOW_AO
            @"
                return float4(pixelFinalColor.rgb * accum, 1.0);
                }
            ";
#else
            @"
                return float4(accum, accum, accum, 1.0);
                }
            ";
#endif

        public const string kAo =
            @"
                static const float kAccumFactor = 3.5f;
                static const float kRadius = 80.0f;
                static const float kTaps = 32.0f;
                static const float kMaxDistanceTolerance = 0.3;
                static const float kMinDistanceTolerance = 0.01;
            " +
            kFragmentPrePre +
            @"
                float contrib(float2 off, vsOut aIn, float3 pixelEyePosition, float3 pixelEyeNormal)
                {
                    float ret = 0.0f;

                    float3 pos = tex2Dproj(MrtSampler2, aIn.TextureLookup + float4(off, 0, 0)).xyz;

                    float3 diff = (pos - pixelEyePosition);
                    float3 n = normalize(diff);

                    float c = dot(n, pixelEyeNormal);
                    float d = dot(pixelEyeNormal, diff);
                    float d2 = length(diff);

                    if (d > kMinDistanceTolerance && d2 < kMaxDistanceTolerance)
                    {
                        float factor = 1.0 - ((d2 - kMinDistanceTolerance) / (kMaxDistanceTolerance - kMinDistanceTolerance));
                        ret = c * (factor * factor);
                    }

                    return ret;
                }
            " +
            kFragmentPrePost +
            @"
                float2 noiseOffset = (JitterDelta.xy * aIn.TextureLookup.w);
                float2 largeOffset = (JitterDelta.xy * aIn.TextureLookup.w) * kRadius;

                float2 scale = 0.5f * (-NearFarFocalLength.zw / pixelEyePosition.z);

                float accum = 0.0;

                float steps = (kTaps / 2);

                for (float i = 0; i < steps; i++)
                {
                    float3 t = tex2Dproj(NoiseSampler, aIn.TextureLookup + float4(noiseOffset * i, 0, 0)).xyz;
    
                    float2 n = t.xy;
                    float2 off = largeOffset * n * ((t.z * 0.5) + 0.5) * scale;

                    accum += contrib(off, aIn, pixelEyePosition, pixelEyeNormal);
                    accum += contrib(-off, aIn, pixelEyePosition, pixelEyeNormal);
                }

                accum *= (kAccumFactor / kTaps);

                return float4(pixelFinalColor.rgb, (1.0 - accum));
                }
            ";
        #endregion

        public static readonly string[] kSources = new string[]
            {
                // Base pass to the backbuffer, with gamma correction.
                kGammaCorrection,

                // Motion blur
                kMb,

                // DofH
                kDofPre +
                kDofHLoop +
                kDofPost,

                // DofV
                kDofPre +
                kDofVLoop +
                kDofPost,

                // Anti-aliasing
                kEdge,

                // BloomMerge
                kBloomMerge,

                // BloomH
                kBloomPre +
                kBloomHLoop +
                kBloomPost,

                // BloomV
                kBloomPre +
                kBloomVLoop +
                kBloomPost,

                // BloomHP
                kBloomHP,

                // Fog
                kFog,

                // Ao
                kAoBlur,
                kAo
            };
        #endregion

        #region Private members
        private static bool msbLoaded = false;
        private static CompiledShader[] msShadersC = new CompiledShader[kSources.Length];

        struct ShaderEntry
        {
            public bool bEnabled;
            public ShaderProfile Profile;
            public PixelShader Shader;
            public bool bFullTarget;
        }

        private static ShaderEntry[] msShaders = new ShaderEntry[kSources.Length];
        private static Vector4[][] msConstants = new Vector4[kSources.Length][];

        private static RenderTarget2D msTargetA = null;
        private static RenderTarget2D msTargetB = null;
        private static RenderTarget2D msSmallTargetA = null;
        private static RenderTarget2D msSmallTargetB = null;

        private static Texture2D msNoiseTexture = null;

        private static Matrix msPrevViewProjection = Matrix.Identity;

        static DeferredPost()
        {
            int count = kSources.Length;
            for (int i = 0; i < count; i++) { msShaders[i].Profile = kShaderProfiles[i]; msShaders[i].bFullTarget = kFullTargets[i]; }
            for (int i = 0; i < count; i++) { msShadersC[i] = ShaderCompiler.CompileFromSource(kSources[i], null, null, CompilerOptions.None, "Fragment", msShaders[i].Profile, TargetPlatform.Windows); }
            for (int i = 0; i < count; i++) { msConstants[i] = new Vector4[kConstantCounts[i]]; }
        }

        private static void _DoPost()
        {
            _States();

            bool bMotionBlur = false;
            if (bEnableMotionBlur &&
                Utilities.AboutEqual(Shared.ViewProjectionTransform, msPrevViewProjection))
            {
                bMotionBlur = true;
                bEnableMotionBlur = false;
            }

            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            float bbwidth = gd.PresentationParameters.BackBufferWidth;
            float bbheight = gd.PresentationParameters.BackBufferHeight;

            Vector4 jitterDelta = new Vector4(1.0f / bbwidth, 1.0f / bbheight, 1.0f / kSmallTargetSize, 1.0f / kSmallTargetSize);

            gd.VertexShader = Deferred.msVertexShader;
            gd.SetVertexShaderConstant(0, Matrix.Identity);
            gd.SetVertexShaderConstant(4, Vector4.One + jitterDelta);

            MeshPart part = siat.UnitQuadMeshPart;
            gd.VertexDeclaration = part.VertexDeclaration;
            gd.Indices = part.Indices;
            gd.Vertices[0].SetSource(part.Vertices, 0, part.VertexStride);
            siat.DrawIndexedSettings.PrimitiveType = part.PrimitiveType;
            siat.DrawIndexedSettings.BaseVertex = 0;
            siat.DrawIndexedSettings.MinVertexIndex = 0;
            siat.DrawIndexedSettings.NumberOfVertices = part.VertexCount;
            siat.DrawIndexedSettings.StartIndex = 0;
            siat.DrawIndexedSettings.PrimitiveCount = part.PrimitiveCount;

            Deferred.SetTargetsAsTextures();
            gd.Textures[Deferred.kCount + 2] = msNoiseTexture;

            Vector4 nearFarFocalLength;
            float fov = Utilities.ExtractFov(Shared.ProjectionTransform);
            Utilities.ExtractNearFar(Shared.ProjectionTransform, out nearFarFocalLength.X, out nearFarFocalLength.Y);
            nearFarFocalLength.W = 1.0f / (float)Math.Tan(fov * 0.5f);
            nearFarFocalLength.Z = nearFarFocalLength.W * (bbheight / bbwidth);

            gd.SetPixelShaderConstant(0, jitterDelta);
            gd.SetPixelShaderConstant(1, nearFarFocalLength);
            gd.SetPixelShaderConstant(2, Matrix.Transpose(Shared.InverseViewTransform));
            gd.SetPixelShaderConstant(6, Matrix.Transpose(Shared.ProjectionTransform));
            gd.SetPixelShaderConstant(10, Matrix.Transpose(Shared.InverseViewTransform * msPrevViewProjection));
            gd.SetPixelShaderConstant(14, new Vector2(bbwidth, bbheight));
            gd.SetPixelShaderConstant(15, new Vector2(1.0f / bbwidth, 1.0f / bbheight));
            gd.SetPixelShaderConstant(16, new Vector4(RenderRoot.Gamma));

            int count = kSources.Length;

            gd.SetRenderTarget(0, msSmallTargetA);
            gd.Clear(ClearOptions.Target, Color.Black, 1.0f, Siat.kDefaultReferenceStencil);

            Utilities.Swap(ref msTargetA, ref msTargetB);
            Utilities.Swap(ref msSmallTargetA, ref msSmallTargetB);

            for (int i = count - 1; i > 0; i--)
            {
                if (!msShaders[i].bEnabled) { continue; }

                gd.PixelShader = msShaders[i].Shader;
                if (msConstants[i].Length > 0) { gd.SetPixelShaderConstant(kFirstCustomConstant, msConstants[i]); }

                gd.SetRenderTarget(0, (msShaders[i].bFullTarget) ? msTargetA : msSmallTargetA);
                gd.Textures[Deferred.kCount] = msTargetB.GetTexture();
                gd.Textures[Deferred.kCount + 1] = msSmallTargetB.GetTexture();
                siat.DrawIndexedPrimitives();

                if (msShaders[i].bFullTarget) { Utilities.Swap(ref msTargetA, ref msTargetB); }
                else { Utilities.Swap(ref msSmallTargetA, ref msSmallTargetB); }
            }

            Debug.Assert(count > 0);
            {
                gd.SetRenderTarget(0, null);
                gd.Textures[Deferred.kCount] = msTargetB.GetTexture();
                gd.Textures[Deferred.kCount + 1] = msSmallTargetB.GetTexture();
                gd.PixelShader = msShaders[0].Shader;
                if (msConstants[0].Length > 0) { gd.SetPixelShaderConstant(kFirstCustomConstant, msConstants[0]); }
                siat.DrawIndexedPrimitives();
            }

            gd.Textures[Deferred.kCount + 2] = null;
            gd.Textures[Deferred.kCount + 1] = null;
            gd.Textures[Deferred.kCount] = null;
            Deferred.UnsetTexturesOfTargets();

            msPrevViewProjection = Shared.ViewProjectionTransform;

            if (bMotionBlur)
            {
                bEnableMotionBlur = true;
            }
        }

        private static void _States()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            RenderState rs = gd.RenderState;

            rs.AlphaBlendEnable = false;
            rs.AlphaTestEnable = false;
            rs.ColorWriteChannels = ColorWriteChannels.All;
            rs.CullMode = Utilities.kBackFaceCulling;
            rs.DepthBias = 0.0f;
            rs.DepthBufferEnable = false;
            rs.DepthBufferWriteEnable = false;
            rs.FillMode = FillMode.Solid;
            rs.StencilEnable = false;

            for (int i = 0; i < Deferred.kCount + 1; i++)
            {
                gd.SamplerStates[i].AddressU = TextureAddressMode.Clamp;
                gd.SamplerStates[i].AddressV = TextureAddressMode.Clamp;
                gd.SamplerStates[i].MagFilter = TextureFilter.Point;
                gd.SamplerStates[i].MinFilter = TextureFilter.Point;
                gd.SamplerStates[i].MipFilter = TextureFilter.None;
            }

            gd.SamplerStates[Deferred.kCount + 1].AddressU = TextureAddressMode.Clamp;
            gd.SamplerStates[Deferred.kCount + 1].AddressV = TextureAddressMode.Clamp;
            gd.SamplerStates[Deferred.kCount + 1].MagFilter = TextureFilter.Linear;
            gd.SamplerStates[Deferred.kCount + 1].MinFilter = TextureFilter.Linear;
            gd.SamplerStates[Deferred.kCount + 1].MipFilter = TextureFilter.None;
        }
        #endregion

        public static void OnLoad()
        {
            if (Deferred.bActive && !msbLoaded)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice gd = siat.GraphicsDevice;

                int width = gd.PresentationParameters.BackBufferWidth;
                int height = gd.PresentationParameters.BackBufferHeight;

                #region Noise texture
                {
                    msNoiseTexture = new Texture2D(gd, width, height, 1, TextureUsage.None, SurfaceFormat.Color);
                    int kNoiseSize = width * height * 4;
                    const float kFactor = 255.0f;
                    byte[] data = new byte[kNoiseSize];
                    Random r = new Random();

                    int indexAng = 0;
                    int indexMag = 0;

                    for (int i = 0; i < kNoiseSize; i += 4)
                    {
                        float ang = (MathHelper.PiOver4 * (float)indexAng) + ((float)r.NextDouble() * MathHelper.PiOver4);
                        float mag = (indexMag * 0.25f) + (float)(r.NextDouble() * 0.25);

                        Vector2 n = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));

                        byte x = (byte)(n.X * kFactor);
                        byte y = (byte)(n.Y * kFactor);
                        byte m = (byte)(mag * kFactor);

                        data[i + 0] = x;
                        data[i + 1] = y;
                        data[i + 2] = m;

                        if (indexAng > 2)
                        {
                            indexAng = 0;
                            indexMag++;

                            if (indexMag > 3) { indexMag = 0; }
                        }
                        else { indexAng++; }
                    }

                    msNoiseTexture.SetData<byte>(data);
                }
                #endregion

                #region Render targets
                msTargetA = new RenderTarget2D(gd, width, height, 1, kFormat);
                msTargetB = new RenderTarget2D(gd, width, height, 1, kFormat);
                msSmallTargetA = new RenderTarget2D(gd, kSmallTargetSize, kSmallTargetSize, 1, kFormat);
                msSmallTargetB = new RenderTarget2D(gd, kSmallTargetSize, kSmallTargetSize, 1, kFormat);
                #endregion

                #region Shaders
                int count = kSources.Length;
                for (int i = 0; i < count; i++)
                {
                    msShaders[i].Shader = new PixelShader(gd, msShadersC[i].GetShaderCode());
                }
                #endregion

                msbLoaded = true;
            }
        }

        public static void OnResize()
        {
            OnUnload();
            OnLoad();
        }

        public static void OnUnload()
        {
            if (msbLoaded)
            {
                #region Shaders
                int count = kSources.Length;
                for (int i = count - 1; i >= 0; i--)
                {
                    msShaders[i].Shader.Dispose(); msShaders[i].Shader = null;
                }
                #endregion

                #region Render target
                msSmallTargetB.Dispose(); msSmallTargetB = null;
                msSmallTargetA.Dispose(); msSmallTargetA = null;
                msTargetB.Dispose(); msTargetB = null;
                msTargetA.Dispose(); msTargetA = null;
                #endregion

                #region Noise texture
                msNoiseTexture.Dispose(); msNoiseTexture = null;
                #endregion

                msbLoaded = false;
            }
        }

        public static void Begin()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            for (int i = gd.GraphicsDeviceCapabilities.MaxSimultaneousRenderTargets - 1; i > 0; i--) { gd.SetRenderTarget(i, null); }
            gd.SetRenderTarget(0, msTargetA);
            gd.Clear(ClearOptions.Target, Color.Black, 1.0f, Siat.kDefaultReferenceStencil);
        }

        public static void End()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;

            _DoPost();
        }

        #region Bloom
        public static bool bEnableBloom
        {
            get
            {
                return msShaders[(int)kShaders.kBloomV].bEnabled;
            }
            
            set
            {
                msShaders[(int)kShaders.kBloomMerge].bEnabled = value;
                msShaders[(int)kShaders.kBloomH].bEnabled = value;
                msShaders[(int)kShaders.kBloomV].bEnabled = value;
                msShaders[(int)kShaders.kBloomHP].bEnabled = value;
            }
        }
        #endregion

        #region Motion Blur
        public static bool bEnableMotionBlur
        {
            get
            {
                return msShaders[(int)kShaders.kMb].bEnabled;
            }

            set
            {
                msShaders[(int)kShaders.kMb].bEnabled = value;
            }
        }
        #endregion

        #region Dof
        public static bool bEnableDof
        {
            get
            {
                return msShaders[(int)kShaders.kDofV].bEnabled;
            }
            
            set
            {
                msShaders[(int)kShaders.kDofH].bEnabled = value;
                msShaders[(int)kShaders.kDofV].bEnabled = value;
            }
        }

        public static Plane DofFocalPlane
        {
            get
            {
                return new Plane(msConstants[(int)kShaders.kDofV][(int)kDofVConstants.kFocalPlane]);
            }

            set
            {
                msConstants[(int)kShaders.kDofV][(int)kDofVConstants.kFocalPlane] = new Vector4(value.Normal, value.D);
            }
        }

        public static float DofHyperfocalDistance
        {
            get
            {
                return msConstants[(int)kShaders.kDofV][(int)kDofVConstants.kHyperfocalDistance].X;
            }

            set
            {
                msConstants[(int)kShaders.kDofV][(int)kDofVConstants.kHyperfocalDistance] = new Vector4(value);
            }
        }
        #endregion

        #region Fog
        public static bool bEnableFog
        {
            get
            {
                return msShaders[(int)kShaders.kFog].bEnabled;
            }
            
            set
            {
                msShaders[(int)kShaders.kFog].bEnabled = value;
            }
        }

        public static Color FogColor
        {
            get
            {
                return new Color(msConstants[(int)kShaders.kFog][(int)kFogConstants.kColor]);
            }

            set
            {
                msConstants[(int)kShaders.kFog][(int)kFogConstants.kColor] = value.ToVector4();
            }
        }

        /// <summary>
        /// [0,1] - Sets the rate of falloff of fog as relative to distance from camera.
        /// </summary>
        public static float FogFalloff
        {
            get
            {
                return msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].X;
            }

            set
            {
                msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].X = Utilities.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Height of the top of the fog plane in world space.
        /// </summary>
        public static float FogHeight
        {
            get
            {
                return msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].Y;
            }

            set
            {
                msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].Y = value;
            }
        }        
        
        /// <summary>
        /// [0,1] - sets the overall density of fog.
        /// </summary>
        public static float FogDensity
        {
            get
            {
                return msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].Z;
            }

            set
            {
                msConstants[(int)kShaders.kFog][(int)kFogConstants.kFalloffHeightDensity].Z = Utilities.Clamp(value, 0.0f, 1.0f);
            }
        }
        #endregion 

        #region Ambient occlusion
        public static bool bEnableAmbientOcclusion
        {
            get
            {
                return msShaders[(int)kShaders.kAo].bEnabled;
            }

            set
            {
                msShaders[(int)kShaders.kAoBlur].bEnabled = value;
                msShaders[(int)kShaders.kAo].bEnabled = value;
            }
        }
        #endregion

        #region Anti-aliasing
        public static bool bEnableAntiAliasing
        {
            get
            {
                return msShaders[(int)kShaders.kAntiAliasing].bEnabled;
            }

            set
            {
                msShaders[(int)kShaders.kAntiAliasing].bEnabled = value;
            }
        }
        #endregion       
    }
}
