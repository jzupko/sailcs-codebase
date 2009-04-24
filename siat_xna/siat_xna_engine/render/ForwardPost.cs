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
using System.Diagnostics;
using System.Text;

using siat.scene;

namespace siat.render
{
    public static class ForwardPost
    {
        public const SurfaceFormat kFormat = SurfaceFormat.HalfVector4;

        #region Shader source
        public const string kGlobals =
            @"
                struct vsIn { float4 Position : POSITION; };
                struct vsOut { float4 Position : POSITION; float4 TextureLookup : TEXCOORD0; };
            ";

        public const string kVertex =
            kGlobals +
            @"
                float4x4 WorldViewProjectionTransform : register(c0);
                float2 ProjFactors : register(c4);

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
                    
                    return ret;
                }
            ";

        public const string kFragment =
            kGlobals +
            @"
                float Gamma : register(c0);

                texture ScreenTexture : register(t0);
                sampler ScreenSampler : register(s0) = sampler_state { texture = <ScreenTexture>; };

                float4 Fragment(vsOut aIn) : COLOR
                {
                    float3 pixelColor = tex2Dproj(ScreenSampler, aIn.TextureLookup).rgb;

                    return float4(pow(pixelColor, (1.0 / Gamma)), 1);
                }
            ";
        #endregion

        #region Private members
        private static bool msbLoaded = false;

        private static PixelShader msFragment = null;
        private static CompiledShader msFragmentC = default(CompiledShader);
        private static VertexShader msVertex = null;
        private static CompiledShader msVertexC = default(CompiledShader);

        private static RenderTarget2D msTarget = null;

        static ForwardPost()
        {
            msFragmentC = ShaderCompiler.CompileFromSource(kFragment, null, null, CompilerOptions.None, "Fragment", ShaderProfile.PS_2_0, TargetPlatform.Windows);
            msVertexC = ShaderCompiler.CompileFromSource(kVertex, null, null, CompilerOptions.None, "Vertex", ShaderProfile.VS_2_0, TargetPlatform.Windows);
        }

        private static void _DoPost()
        {
            _States();

            Siat siat = Siat.Singleton;
            GraphicsDevice gd = siat.GraphicsDevice;

            float bbwidth = gd.PresentationParameters.BackBufferWidth;
            float bbheight = gd.PresentationParameters.BackBufferHeight;

            Vector2 jitterDelta = new Vector2(1.0f / bbwidth, 1.0f / bbheight);

            gd.VertexShader = msVertex;
            gd.SetVertexShaderConstant(0, Matrix.Identity);
            gd.SetVertexShaderConstant(4, Vector2.One + jitterDelta);

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

            gd.PixelShader = msFragment;
            gd.SetPixelShaderConstant(0, new Vector4(RenderRoot.Gamma));
            gd.Textures[0] = msTarget.GetTexture();            
            siat.DrawIndexedPrimitives();
            gd.Textures[0] = null;
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
            
            gd.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
            gd.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
            gd.SamplerStates[0].MagFilter = TextureFilter.Point;
            gd.SamplerStates[0].MinFilter = TextureFilter.Point;
            gd.SamplerStates[0].MipFilter = TextureFilter.None;
        }
        #endregion

        public static void OnLoad()
        {
            if (!Deferred.bActive && !msbLoaded)
            {
                Siat siat = Siat.Singleton;
                GraphicsDevice gd = siat.GraphicsDevice;

                #region Render target
                int width = gd.PresentationParameters.BackBufferWidth;
                int height = gd.PresentationParameters.BackBufferHeight;

                msTarget = new RenderTarget2D(gd, width, height, 1, kFormat, 
                    gd.PresentationParameters.MultiSampleType, gd.PresentationParameters.MultiSampleQuality);
                #endregion

                #region Shaders
                msFragment = new PixelShader(gd, msFragmentC.GetShaderCode());
                msVertex = new VertexShader(gd, msVertexC.GetShaderCode());
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
                msFragment.Dispose(); msFragment = null;
                msVertex.Dispose(); msVertex = null;
                #endregion

                #region Render target
                msTarget.Dispose(); msTarget = null;
                #endregion

                msbLoaded = false;
            }
        }

        public static void Begin()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            gd.SetRenderTarget(0, msTarget);
            gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil | ClearOptions.Target, RenderRoot.ClearColor, 1.0f, Siat.kDefaultReferenceStencil);
        }

        public static void End()
        {
            GraphicsDevice gd = Siat.Singleton.GraphicsDevice;
            gd.SetRenderTarget(0, null);

            _DoPost();
        }
    }
}
