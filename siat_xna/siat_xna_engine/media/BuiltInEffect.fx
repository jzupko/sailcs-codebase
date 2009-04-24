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

#include "..\..\siat_xna_cp\impl\collada_effect_common.h"

//-----------------------------------------------------------------------------
// standard input constants
//-----------------------------------------------------------------------------
float4 SkinningTransforms[kSkinningMatricesSize] : siat_SkinningTransforms;
float ShadowFarDepth : siat_ShadowRange;
float4x4 ViewProjectionTransform : siat_ViewProjectionTransform;
float4x4 ViewTransform : siat_ViewTransform;
float4x4 WorldTransform : siat_WorldTransform;

//-----------------------------------------------------------------------------
// inputs outputs
//-----------------------------------------------------------------------------
struct vsIn
{
	float4 Position : POSITION;
};

struct vsInAnimated
{
	float4 Position : POSITION;
	float4 BoneIndices : BLENDINDICES;
	float4 BoneWeights : BLENDWEIGHT;
};

struct vsOut
{
	float4 Position : POSITION;
};

struct vsOutShadow
{
	float4 Position : POSITION;
	float3 ViewPosition : TEXCOORD0;
};

//-----------------------------------------------------------------------------
// helper functions
//-----------------------------------------------------------------------------
float4x4 _UnpackTransform4x4(float aIndex, float4 m[kSkinningMatricesSize])
{
	float i = aIndex * 3.0f;
	
	return float4x4(m[i+0].x, m[i+1].x, m[i+2].x, 0,
	                m[i+0].y, m[i+1].y, m[i+2].y, 0,
	                m[i+0].z, m[i+1].z, m[i+2].z, 0,
	                m[i+0].w, m[i+1].w, m[i+2].w, 1);
}

float4x4 _GetTransform4x4(vsInAnimated aIn, float4 m[kSkinningMatricesSize])
{
	float4x4 ret = aIn.BoneWeights.x * _UnpackTransform4x4(aIn.BoneIndices.x, m);
	ret += aIn.BoneWeights.y * _UnpackTransform4x4(aIn.BoneIndices.y, m);
	ret += aIn.BoneWeights.z * _UnpackTransform4x4(aIn.BoneIndices.z, m);
	ret += aIn.BoneWeights.w * _UnpackTransform4x4(aIn.BoneIndices.w, m);
	
	return ret;	
}
	
float4x4 GetSkinningWorldTransform(vsInAnimated aIn)
{
	return mul(_GetTransform4x4(aIn, SkinningTransforms), WorldTransform);
}

//-----------------------------------------------------------------------------
// vertex shaders
//-----------------------------------------------------------------------------
vsOutShadow VertexAnimatedShadow(vsInAnimated aIn)
{
	vsOutShadow output;
	
	float4 world = mul(aIn.Position, GetSkinningWorldTransform(aIn));
	output.Position = mul(world, ViewProjectionTransform);
	output.ViewPosition = mul(world, ViewTransform);

	return output;
}

vsOutShadow VertexShadow(vsIn aIn)
{
	vsOutShadow output;
	
	float4 world = mul(aIn.Position, WorldTransform);
	output.Position = mul(world, ViewProjectionTransform);
	output.ViewPosition = mul(world, ViewTransform);
	
	return output;
}

vsOut VertexSimple(vsIn aIn)
{
	vsOut output;
	
	float4 world = mul(aIn.Position, WorldTransform);
	output.Position = mul(world, ViewProjectionTransform);
	
	return output;
}

//-----------------------------------------------------------------------------
// fragment shaders
//-----------------------------------------------------------------------------
float4 FragmentShadowDepth(vsOutShadow aIn) : COLOR
{
	float l = length(aIn.ViewPosition);
	float ln = (l / ShadowFarDepth);
	
	float m = max(abs(ddx(aIn.ViewPosition.z)), abs(ddy(aIn.ViewPosition.z)));
	
	float linearDepth = saturate(ln + (kShadowSlopeBias * m));
	
	return float4(linearDepth, 0, 0, 0);
}

float4 FragmentSimple(vsOut aIn) : COLOR
{
	return float4(0, 0, 0, 1);
}

technique siat_RenderOcclusionQuery
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = 0;
		CullMode = BACK_FACE_CULLING;
		FillMode = Solid;
		ZWriteEnable = false;
	
		VertexShader = compile vs_2_0 VertexSimple();
		PixelShader = compile ps_2_0 FragmentSimple();
	}
}

technique siat_RenderPortal
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = 0;
		CullMode = BACK_FACE_CULLING;
		FillMode = Solid;
		ZWriteEnable = false;
	
		VertexShader = compile vs_2_0 VertexSimple();
		PixelShader = compile ps_2_0 FragmentSimple();
	}
}

technique siat_RenderShadowDepth
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA;
		CullMode = BACK_FACE_CULLING;
		FillMode = Solid;
		ZWriteEnable = true;
			
		VertexShader = compile vs_2_a VertexShadow();
		PixelShader = compile ps_2_a FragmentShadowDepth();
	}
}

technique siat_RenderAnimatedShadowDepth
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA;
		CullMode = BACK_FACE_CULLING;
		FillMode = Solid;
		ZWriteEnable = true;
			
		VertexShader = compile vs_2_a VertexAnimatedShadow();
		PixelShader = compile ps_2_a FragmentShadowDepth();
	}
}

technique siat_RenderSolid
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA;
		CullMode = BACK_FACE_CULLING;
		FillMode = Solid;
		ZWriteEnable = true;
	
		VertexShader = compile vs_2_0 VertexSimple();
		PixelShader = compile ps_2_0 FragmentSimple();
	}
}

technique siat_RenderWireframe
{
	pass
	{
		AlphaBlendEnable = false;
		AlphaTestEnable = false;
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA;
		CullMode = BACK_FACE_CULLING;
		FillMode = Wireframe;
		ZWriteEnable = true;
	
		VertexShader = compile vs_2_0 VertexSimple();
		PixelShader = compile ps_2_0 FragmentSimple();
	}
}