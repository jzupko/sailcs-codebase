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

#include "collada_effect_common.h"

//-----------------------------------------------------------------------------
// standard input constants
//-----------------------------------------------------------------------------
#if defined(ANIMATED)
	float4 SkinningTransforms[kSkinningMatricesSize] : siat_SkinningTransforms;
#endif

float Gamma : siat_Gamma;
float4x4 InverseViewTransform : siat_InverseViewTransform;
float4x4 InverseTransposeWorldTransform : siat_InverseTransposeWorldTransform;
float3 LightAttenuation : siat_LightAttenuation;
float3 LightDiffuse : siat_LightDiffuse;
float3 LightPositionOrDirection : siat_LightPositionOrDirection;
float3 LightSpecular : siat_LightSpecular;
float4 PickingColor : siat_PickingColor;
float ShadowFarDepth : siat_ShadowRange;
texture ShadowTexture : siat_ShadowTexture;
float4x4 ShadowTransform : siat_ShadowTransform;
float3 SpotDirection : siat_SpotDirection;
float SpotFalloffCosAngle : siat_SpotCutoffCosHalfAngle;
float SpotFalloffExponent : siat_SpotFalloffExponent;
float4x4 ViewTransform : siat_ViewTransform;
float4x4 ViewProjectionTransform : siat_ViewProjectionTransform;
float4x4 WorldTransform : siat_WorldTransform;

sampler ShadowSampler = sampler_state
{
	texture = <ShadowTexture>;
	AddressU = clamp;
	AddressV = clamp;
	MinFilter = POINT;
	MagFilter = POINT;
	MipFilter = NONE;
};

//-----------------------------------------------------------------------------
// helper macros
//-----------------------------------------------------------------------------
#if defined(DIFFUSE_COLOR) || defined(DIFFUSE_TEXTURE) || defined(DIFFUSE_VERTEX)
#	define DIFFUSE
#endif

#if defined(AMBIENT_COLOR) || defined(AMBIENT_TEXTURE)
#	define AMBIENT
#endif

#if defined(EMISSION_COLOR) || defined(EMISSION_TEXTURE)
#	define EMISSION
#endif

#if defined(TRANSPARENT_COLOR) || defined(TRANSPARENT_TEXTURE)
#	define TRANSPARENT
#endif

#if defined(REFLECTIVE_COLOR) || defined(REFLECTIVE_TEXTURE)
#	define REFLECTIVE
#endif

#if defined(SPECULAR_COLOR) || defined(SPECULAR_TEXTURE)
#	define SPECULAR
#endif

#if defined(BUMP_TEXTURE)
#	define BUMP
#endif

//-----------------------------------------------------------------------------
// generated-at-content-build-time constants
//-----------------------------------------------------------------------------
#if defined(EMISSION_COLOR)
	float4 EmissionColor : EMISSION_COLOR;
#elif defined(EMISSION_TEXTURE)
	texture EmissionTexture : EMISSION_TEXTURE;
	
	sampler EmissionSampler = sampler_state
	{
		AddressU = EMISSION_ADDRESSU;
		AddressV = EMISSION_ADDRESSV;
		AddressW = EMISSION_ADDRESSW;
		MinFilter = EMISSION_MIN_FILTER;
		MagFilter = EMISSION_MAG_FILTER;
		MipFilter = EMISSION_MIP_FILTER;
        BorderColor = EMISSION_BORDER_COLOR;
        MaxMipLevel = EMISSION_MAX_MIP_LEVEL;
        MipMapLodBias = EMISSION_MIP_MAP_LOD_BIAS;		
		Texture = (EmissionTexture);
	};
#endif

#if defined(REFLECTIVE_COLOR)
	float4 ReflectiveColor : REFLECTIVE_COLOR;
#elif defined(REFLECTIVE_TEXTURE)	
	texture ReflectiveTexture : REFLECTIVE_TEXTURE;
	
	sampler ReflectiveSampler = sampler_state
	{
		AddressU = REFLECTIVE_ADDRESSU;
		AddressV = REFLECTIVE_ADDRESSV;
		AddressW = REFLECTIVE_ADDRESSW;
		MinFilter = REFLECTIVE_MIN_FILTER;
		MagFilter = REFLECTIVE_MAG_FILTER;
		MipFilter = REFLECTIVE_MIP_FILTER;
        BorderColor = REFLECTIVE_BORDER_COLOR;
        MaxMipLevel = REFLECTIVE_MAX_MIP_LEVEL;
        MipMapLodBias = REFLECTIVE_MIP_MAP_LOD_BIAS;			
		Texture = (ReflectiveTexture);
	};	
#endif

#if defined(REFLECTIVE)
	float Reflectivity : REFLECTIVITY;
#endif

#if defined(TRANSPARENT_COLOR)
	float4 TransparentColor : TRANSPARENT_COLOR;
#elif defined(TRANSPARENT_TEXTURE)
	texture TransparentTexture : TRANSPARENT_TEXTURE;
	
	sampler TransparentSampler = sampler_state
	{
		AddressU = TRANSPARENT_ADDRESSU;
		AddressV = TRANSPARENT_ADDRESSV;
		AddressW = TRANSPARENT_ADDRESSW;
		MinFilter = TRANSPARENT_MIN_FILTER;
		MagFilter = TRANSPARENT_MAG_FILTER;
		MipFilter = TRANSPARENT_MIP_FILTER;
        BorderColor = TRANSPARENT_BORDER_COLOR;
        MaxMipLevel = TRANSPARENT_MAX_MIP_LEVEL;
        MipMapLodBias = TRANSPARENT_MIP_MAP_LOD_BIAS;		
		Texture = (TransparentTexture);
	};	
#endif

#if defined(TRANSPARENT)
	float Transparency : TRANSPARENCY;
#endif

#if defined(AMBIENT_COLOR)
	float4 AmbientColor : AMBIENT_COLOR;
#elif defined(AMBIENT_TEXTURE)
	texture AmbientTexture : AMBIENT_TEXTURE;
	
	sampler AmbientSampler = sampler_state
	{
		AddressU = AMBIENT_ADDRESSU;
		AddressV = AMBIENT_ADDRESSV;
		AddressW = AMBIENT_ADDRESSW;
		MinFilter = AMBIENT_MIN_FILTER;
		MagFilter = AMBIENT_MAG_FILTER;
		MipFilter = AMBIENT_MIP_FILTER;
        BorderColor = AMBIENT_BORDER_COLOR;
        MaxMipLevel = AMBIENT_MAX_MIP_LEVEL;
        MipMapLodBias = AMBIENT_MIP_MAP_LOD_BIAS;			
		Texture = (AmbientTexture);
	};	
#endif

#if defined(DIFFUSE_COLOR)
	float4 DiffuseColor : DIFFUSE_COLOR;
#elif defined(DIFFUSE_TEXTURE)
	texture DiffuseTexture : DIFFUSE_TEXTURE;
	
	sampler DiffuseSampler = sampler_state
	{
		AddressU = DIFFUSE_ADDRESSU;
		AddressV = DIFFUSE_ADDRESSV;
		AddressW = DIFFUSE_ADDRESSW;
		MinFilter = DIFFUSE_MIN_FILTER;
		MagFilter = DIFFUSE_MAG_FILTER;
		MipFilter = DIFFUSE_MIP_FILTER;
        BorderColor = DIFFUSE_BORDER_COLOR;
        MaxMipLevel = DIFFUSE_MAX_MIP_LEVEL;
        MipMapLodBias = DIFFUSE_MIP_MAP_LOD_BIAS;		
		Texture = (DiffuseTexture);
	};
#endif

#if defined(SPECULAR_COLOR)
	float4 SpecularColor : SPECULAR_COLOR;
#elif defined(SPECULAR_TEXTURE)
	texture SpecularTexture : SPECULAR_TEXTURE;
	
	sampler SpecularSampler = sampler_state
	{
		AddressU = SPECULAR_ADDRESSU;
		AddressV = SPECULAR_ADDRESSV;
		AddressW = SPECULAR_ADDRESSW;
		MinFilter = SPECULAR_MIN_FILTER;
		MagFilter = SPECULAR_MAG_FILTER;
		MipFilter = SPECULAR_MIP_FILTER;
        BorderColor = SPECULAR_BORDER_COLOR;
        MaxMipLevel = SPECULAR_MAX_MIP_LEVEL;
        MipMapLodBias = SPECULAR_MIP_MAP_LOD_BIAS;		
		Texture = (SpecularTexture);
	};	
#endif

#if defined(SPECULAR)
	float Shininess : SHININESS;
#endif

#if defined(INDEX_OF_REFRACTION)
	float IndexOfRefraction;
#endif

#if defined(BUMP_TEXTURE)
	texture BumpTexture : BUMP_TEXTURE;
	
	sampler BumpSampler = sampler_state
	{
		AddressU = BUMP_ADDRESSU;
		AddressV = BUMP_ADDRESSV;
		AddressW = BUMP_ADDRESSW;
		MinFilter = BUMP_MIN_FILTER;
		MagFilter = BUMP_MAG_FILTER;
		MipFilter = BUMP_MIP_FILTER;
        BorderColor = BUMP_BORDER_COLOR;
        MaxMipLevel = BUMP_MAX_MIP_LEVEL;
        MipMapLodBias = BUMP_MIP_MAP_LOD_BIAS;		
		Texture = (BumpTexture);
	};	
#endif

//-----------------------------------------------------------------------------
// inputs outputs
//-----------------------------------------------------------------------------
struct vsIn
{
	float3 Normal : NORMAL;
	float4 Position : POSITION;

#	if defined(DIFFUSE_VERTEX)
		float4 DiffuseColor : COLOR0;
#	endif

#	if defined(ANIMATED)
		float4 BoneIndices : BLENDINDICES;
		float4 BoneWeights : BLENDWEIGHT;
#	endif

#	if defined(BUMP)
		float3 Tangent : TANGENT;
#	endif

#   if (TEXCOORDS_COUNT > 0)
		float2 Texcoords0 : TEXCOORD0;
#	endif
#   if (TEXCOORDS_COUNT > 1)
		float2 Texcoords1 : TEXCOORD1;
#	endif
#   if (TEXCOORDS_COUNT > 2)
		float2 Texcoords2 : TEXCOORD2;
#	endif
#   if (TEXCOORDS_COUNT > 3)
		float2 Texcoords3 : TEXCOORD3;
#	endif
#   if (TEXCOORDS_COUNT > 4)
		float2 Texcoords4 : TEXCOORD4;
#	endif
#   if (TEXCOORDS_COUNT > 5)
		float2 Texcoords5 : TEXCOORD5;
#	endif
#   if (TEXCOORDS_COUNT > 6)
		float2 Texcoords6 : TEXCOORD6;
#	endif
#   if (TEXCOORDS_COUNT > 7)
		float2 Texcoords7 : TEXCOORD7;
#	endif

};

struct vsOutPicking
{
	float4 Position : POSITION;
	float4 PositionH : TEXCOORD0;
	
#	if defined(TRANSPARENT_TEXTURE)
		float2 TransparentTexCoords : TEXCOORD1;
#	endif	
};

struct vsOutDeferred
{
	float4 Position : POSITION;
	float4 PositionFragment : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	
#	if defined(DIFFUSE_TEXTURE) || defined(REFLECTIVE_TEXTURE)
		float4 DiffuseReflectiveTexCoords : TEXCOORD2;
#	endif
#	if defined(SPECULAR_TEXTURE) || defined(BUMP_TEXTURE)
		float4 SpecularBumpTexCoords : TEXCOORD3;
#	endif
#	if defined(TRANSPARENT_TEXTURE)
		float2 TransparentTexCoords : TEXCOORD4;
#	endif
};

struct vsOutBase
{
	float4 Position : POSITION;
	
#	if defined(DIFFUSE_TEXTURE) || defined(AMBIENT_TEXTURE)
		float4 DiffuseAmbientTexCoords : TEXCOORD0;
#	endif	
	
#	if defined(EMISSION_TEXTURE) || defined(TRANSPARENT_TEXTURE)
		float4 EmissionTransparentTexCoords : TEXCOORD1;
#	endif

#	if defined(REFLECTIVE_TEXTURE)
		float2 ReflectiveTexCoords : TEXCOORD2;
#	endif

};

struct vsOut
{
	float4 Position : POSITION;
	float3 Eye : TEXCOORD0;	
	float3 Light : TEXCOORD1;
	float3 Normal : TEXCOORD2;
	float4 ShadowTexCoords : TEXCOORD3;

#	if defined(DIFFUSE_TEXTURE) || defined(TRANSPARENT_TEXTURE)
		float4 DiffuseTransparentTexCoords : TEXCOORD4;
#	endif
#	if defined(REFLECTIVE_TEXTURE) || defined(SPECULAR_TEXTURE)
		float4 ReflectiveSpecularTexCoords : TEXCOORD5;
#	endif
#	if defined(BUMP_TEXTURE)
		float2 BumpTexCoords : TEXCOORD6;
#	endif

#	if defined(DIFFUSE_VERTEX)
		float4 DiffuseColor : TEXCOORD7;
#	endif

};

//-----------------------------------------------------------------------------
// functions
//-----------------------------------------------------------------------------
#if defined(ANIMATED)
    // unpack skinning transforms (which are stored in column-major form) into
    // row-major form.
	float3x3 _UnpackTransform3x3(float aIndex, float4 m[kSkinningMatricesSize])
	{
		float i = aIndex * 3.0f;
		
		return float3x3(m[i+0].x, m[i+1].x, m[i+2].x,
		                m[i+0].y, m[i+1].y, m[i+2].y,
		                m[i+0].z, m[i+1].z, m[i+2].z);
	}
	
	float3x3 _GetTransform3x3(vsIn aIn, float4 m[kSkinningMatricesSize])
	{
		float3x3 ret = aIn.BoneWeights.x * _UnpackTransform3x3(aIn.BoneIndices.x, m);
		ret += aIn.BoneWeights.y * _UnpackTransform3x3(aIn.BoneIndices.y, m);
		ret += aIn.BoneWeights.z * _UnpackTransform3x3(aIn.BoneIndices.z, m);
		ret += aIn.BoneWeights.w * _UnpackTransform3x3(aIn.BoneIndices.w, m);
		
		return ret;	
	}

	float4x4 _UnpackTransform4x4(float aIndex, float4 m[kSkinningMatricesSize])
	{
		float i = aIndex * 3.0f;
		
		return float4x4(m[i+0].x, m[i+1].x, m[i+2].x, 0,
		                m[i+0].y, m[i+1].y, m[i+2].y, 0,
		                m[i+0].z, m[i+1].z, m[i+2].z, 0,
		                m[i+0].w, m[i+1].w, m[i+2].w, 1);
	}
	
	float4x4 _GetTransform4x4(vsIn aIn, float4 m[kSkinningMatricesSize])
	{
		float4x4 ret = aIn.BoneWeights.x * _UnpackTransform4x4(aIn.BoneIndices.x, m);
		ret += aIn.BoneWeights.y * _UnpackTransform4x4(aIn.BoneIndices.y, m);
		ret += aIn.BoneWeights.z * _UnpackTransform4x4(aIn.BoneIndices.z, m);
		ret += aIn.BoneWeights.w * _UnpackTransform4x4(aIn.BoneIndices.w, m);
	
		return ret;	
	}
#endif

// Note: animated objects only have a WorldTransform array, no inverse tranpose.
// Non-orthogonal transforms for this geometry will result in incorrect lighting.
float3x3 GetInverseTransposeWorldTransform(vsIn aIn)
{
#	if defined(ANIMATED)
		return mul(_GetTransform3x3(aIn, SkinningTransforms), (float3x3)InverseTransposeWorldTransform);
#	else
		return (float3x3)InverseTransposeWorldTransform;
#	endif
}

float4x4 GetWorldTransform(vsIn aIn)
{
#	if defined(ANIMATED)
		return mul(_GetTransform4x4(aIn, SkinningTransforms), WorldTransform);
#	else
		return WorldTransform;
#	endif
}

#if defined(BUMP)
float3x3 GetTangentToWorldTransform(float3 aNormal, float3 aTangent, float3x3 aITWorldTransform)
{
	float3 binormal = cross(aNormal, aTangent);
	
	float3 t = normalize(mul(aTangent, aITWorldTransform)); // row 0
	float3 b = normalize(mul(binormal, aITWorldTransform)); // row 1
	float3 n = normalize(mul(aNormal, aITWorldTransform)); // row 2
	
	return float3x3(t.x, b.x, n.x,
					t.y, b.y, n.y,
					t.z, b.z, n.z);
}

float3x3 GetWorldToTangentTransform(float3 aNormal, float3 aTangent, float3x3 aITWorldTransform)
{
	float3x3 ret;
	float3 binormal = cross(aNormal, aTangent);
	
	ret[0] = normalize(mul(aTangent, aITWorldTransform)); // row 0
	ret[1] = normalize(mul(binormal, aITWorldTransform)); // row 1
	ret[2] = normalize(mul(aNormal, aITWorldTransform)); // row 2
	
	return ret;
}
#endif

void LightTerms(vsIn aIn, float4 aWorld, uniform bool abDirectional, uniform bool abPoint, uniform bool abSpot, uniform bool abShadow, 
	out float3 arEye, out float3 arLight, out float3 arNormal, out float4 arShadowTexCoords)
{
	if (abShadow) { arShadowTexCoords = mul(aWorld, ShadowTransform); }
	else { arShadowTexCoords = float4(0, 0, 0, 0); }
	
	float3 eyePos = float3(InverseViewTransform._41, InverseViewTransform._42, InverseViewTransform._43);
	
	if (abSpot || abPoint) { arLight = LightPositionOrDirection - aWorld.xyz; }
	else  {	 arLight = -LightPositionOrDirection; }
	
	float3x3 itWorld = GetInverseTransposeWorldTransform(aIn);
	arEye = eyePos - aWorld.xyz;
	
#	if defined(BUMP)
		float3x3 worldToTangent = GetWorldToTangentTransform(aIn.Normal, aIn.Tangent, itWorld);
		arEye = mul(arEye, worldToTangent);
		arLight = mul(arLight, worldToTangent);
		arNormal = float3(0, 0, 0); // remove the output normal if there is a bump-map.
#	else
		arNormal = mul(aIn.Normal, itWorld);
#	endif	
}

float Shadow(float4 aShadowTexCoords, float aPixelDepth, uniform bool abFiltered)
{
	float ret = 0.0f;

    float offset = (kShadowDelta * aShadowTexCoords.w);
    float noffset = -offset;

	if (abFiltered)
	{
		float4 shadowDepths;
		shadowDepths.x = tex2Dproj(ShadowSampler, aShadowTexCoords + float4(noffset, noffset, 0, 0)).x;
		shadowDepths.y = tex2Dproj(ShadowSampler, aShadowTexCoords + float4( offset, noffset, 0, 0)).x;
		shadowDepths.z = tex2Dproj(ShadowSampler, aShadowTexCoords + float4(noffset,  offset, 0, 0)).x;
		shadowDepths.w = tex2Dproj(ShadowSampler, aShadowTexCoords + float4( offset,  offset, 0, 0)).x;

		float4 c = (aPixelDepth <= shadowDepths);
		
		ret = (c.x + c.y + c.z + c.w) * 0.25;
	}
	else
	{
		float shadowDepth = tex2Dproj(ShadowSampler, aShadowTexCoords).x;
		
		if (aPixelDepth <= shadowDepth) { ret = 1.0f; }
	}
	
	return ret;
}

// Note: the ( > ) ? : checks are necessary here but (apparently) not in GammaTextureRead
// because the Effect compiler compiles this into a pre-shader and it (apparently)
// handles (0^n) (n checked at values of 2.2 and 1) differently than my GPU hardware does
// (Nvidia 9800). The preshader produces 1 for these cases instead of 0. 0 is mathematically
// correct.
float4 GammaColor(float4 aColor)
{
	float4 ret;
	ret.r = (aColor.r >= kLooseTolerance) ? pow(aColor.r, Gamma) : 0.0;
	ret.g = (aColor.g >= kLooseTolerance) ? pow(aColor.g, Gamma) : 0.0;
	ret.b = (aColor.b >= kLooseTolerance) ? pow(aColor.b, Gamma) : 0.0;
	ret.a = aColor.a;
	
	return ret;
}

float4 GammaTextureRead(sampler aSampler, float2 aTexCoords)
{
	float4 col = tex2D(aSampler, aTexCoords);
	
	return float4(pow(col.rgb, Gamma), col.a);
}

//-----------------------------------------------------------------------------
// vertex shaders
//-----------------------------------------------------------------------------
// special vertex shader, used for rendering picking masks.
vsOutPicking VertexPicking(vsIn aIn)
{
	vsOutPicking output;
	
	float4 world = mul(aIn.Position, GetWorldTransform(aIn));
	output.Position = mul(world, ViewProjectionTransform);
	output.PositionH = output.Position;
	
#if defined(TRANSPARENT_TEXTURE)
	output.TransparentTexCoords = aIn.TRANSPARENT_TEXCOORDS;
#endif	
	
	return output;
}

// base vertex shader, used during unlit base pass (affected by ambient, emission)
vsOutBase VertexBase(vsIn aIn)
{
	vsOutBase output;

	float4 world = mul(aIn.Position, GetWorldTransform(aIn));
	output.Position = mul(world, ViewProjectionTransform);

#	if defined(DIFFUSE_TEXTURE)
		output.DiffuseAmbientTexCoords.xy = aIn.DIFFUSE_TEXCOORDS;
#	elif defined(AMBIENT_TEXTURE)
		output.DiffuseAmbientTexCoords.xy = float2(0, 0);
#	endif
#	if defined(AMBIENT_TEXTURE)
		output.DiffuseAmbientTexCoords.zw = aIn.AMBIENT_TEXCOORDS;
#	elif defined(DIFFUSE_TEXTURE)
		output.DiffuseAmbientTexCoords.zw = float2(0, 0);
#	endif

#	if defined(EMISSION_TEXTURE)
		output.EmissionTransparentTexCoords.xy = aIn.EMISSION_TEXCOORDS;
#	elif defined(TRANSPARENT_TEXTURE)
		output.EmissionTransparentTexCoords.xy = float2(0, 0);
#	endif
#	if defined(TRANSPARENT_TEXTURE)
		output.EmissionTransparentTexCoords.zw = aIn.TRANSPARENT_TEXCOORDS;
#	elif defined(EMISSION_TEXTURE)
		output.EmissionTransparentTexCoords.zw = float2(0, 0);
#	endif

#	if defined(REFLECTIVE_TEXTURE)
		output.ReflectiveTexCoords = aIn.REFLECTIVE_TEXCOORDS;
#	endif
	
	return output;
}

// main vertex shading, used when lighting is applied.
vsOut Vertex(vsIn aIn, uniform bool abDirectional, uniform bool abPoint, uniform bool abSpot, uniform bool abShadow)
{
	float4 world = mul(aIn.Position, GetWorldTransform(aIn));

	vsOut output;

	LightTerms(aIn, world, abDirectional, abPoint, abSpot, abShadow, 
		output.Eye, output.Light, output.Normal, output.ShadowTexCoords);

	output.Position = mul(world, ViewProjectionTransform);
	
#	if defined(DIFFUSE_TEXTURE)
		output.DiffuseTransparentTexCoords.xy = aIn.DIFFUSE_TEXCOORDS;
#	elif defined(TRANSPARENT_TEXTURE)
		output.DiffuseTransparentTexCoords.xy = float2(0, 0);
#	endif
#	if defined(TRANSPARENT_TEXTURE)
		output.DiffuseTransparentTexCoords.zw = aIn.TRANSPARENT_TEXCOORDS;
#	elif defined(DIFFUSE_TEXTURE)
		output.DiffuseTransparentTexCoords.zw = float2(0, 0);		
#	endif

#	if defined(REFLECTIVE_TEXTURE)
		output.ReflectiveSpecularTexCoords.xy = aIn.REFLECTIVE_TEXCOORDS;
#	elif defined(SPECULAR_TEXTURE)
		output.ReflectiveSpecularTexCoords.xy = float2(0, 0);
#	endif
#	if defined(SPECULAR_TEXTURE)
		output.ReflectiveSpecularTexCoords.zw = aIn.SPECULAR_TEXCOORDS;
#	elif defined(REFLECTIVE_TEXTURE)
		output.ReflectiveSpecularTexCoords.zw = float2(0, 0);
#	endif
			
#	if defined(BUMP_TEXTURE)
		output.BumpTexCoords = aIn.BUMP_TEXCOORDS;
#	endif

#	if defined (DIFFUSE_VERTEX)
		output.DiffuseColor = aIn.DiffuseColor;
#	endif
			
	return output;
}

vsOutDeferred VertexDeferred(vsIn aIn)
{
	float4 world = mul(aIn.Position, GetWorldTransform(aIn));

	vsOutDeferred output;

#	if defined(BUMP)
		output.Normal = float3(0, 0, 0); // remove the output normal if there is a bump-map.
#	else
		float3x3 m = mul(GetInverseTransposeWorldTransform(aIn), (float3x3)ViewTransform);
		output.Normal = mul(aIn.Normal, m);
#	endif

	output.Position = mul(world, ViewProjectionTransform);
	output.PositionFragment = mul(world, ViewTransform);
	
#	if defined(DIFFUSE_TEXTURE)
		output.DiffuseReflectiveTexCoords.xy = aIn.DIFFUSE_TEXCOORDS;
#	elif defined(REFLECTIVE_TEXTURE)
		output.DiffuseReflectiveTexCoords.xy = float2(0, 0);
#	endif

#	if defined(REFLECTIVE_TEXTURE)
		output.DiffuseReflectiveTexCoords.zw = aIn.REFLECTIVE_TEXCOORDS;
#	elif defined(DIFFUSE_TEXTURE)
		output.DiffuseReflectiveTexCoords.zw = float2(0, 0);
#	endif

#	if defined(SPECULAR_TEXTURE)
		output.SpecularBumpTexCoords.xy = aIn.SPECULAR_TEXCOORDS;
#	elif defined(BUMP_TEXTURE)
		output.SpecularBumpTexCoords.xy = float2(0, 0);
#	endif
#	if defined(BUMP_TEXTURE)
		output.SpecularBumpTexCoords.zw = aIn.BUMP_TEXCOORDS;
#	elif defined(SPECULAR_TEXTURE)
		output.SpecularBumpTexCoords.zw = float2(0, 0);
#	endif

#	if defined(TRANSPARENT_TEXTURE)
		output.TransparentTexCoords = aIn.TRANSPARENT_TEXCOORDS;
#	endif

	return output;
}

//-----------------------------------------------------------------------------
// fragment shaders
//-----------------------------------------------------------------------------
float4 FragmentPicking(vsOutPicking aIn) : COLOR
{
#	if defined(TRANSPARENT_COLOR)
		float4 transparent = TransparentColor;
#	elif defined(TRANSPARENT_TEXTURE)
		float4 transparent = tex2D(TransparentSampler, aIn.TransparentTexCoords.xy);
#	endif
#	if defined(TRANSPARENT)
		float alpha = 1.0f;

#		if defined(ALPHA_ONE)
			alpha = transparent.a * Transparency;
#		elif defined(RGB_ZERO)
			alpha = 1.0f - (((0.212671 * transparent.r) + (0.715160 * transparent.g) + (0.072169 * transparent.b)) * Transparency);
#		endif	

		clip(alpha - OPAQUE_OF_TRANSPARENCY_F);
#	endif

	return float4(PickingColor.rgb, (aIn.PositionH.z / aIn.PositionH.w));
}

float4 FragmentBase(vsOutBase aIn) : COLOR
{
	float alpha = 1.0f;

//---- Get diffuse color.
#	if defined(DIFFUSE_COLOR)
		float3 diffuse = GammaColor(DiffuseColor).rgb;
#	elif defined(DIFFUSE_TEXTURE)
		float3 diffuse = GammaTextureRead(DiffuseSampler, aIn.DiffuseAmbientTexCoords.xy).rgb;
#	endif

//---- Get ambient color.
#	if defined(AMBIENT_COLOR)
		float3 ambient = GammaColor(AmbientColor).rgb;
#	elif defined(AMBIENT_TEXTURE)
		float3 ambient = GammaTextureRead(AmbientSampler, aIn.DiffuseAmbientTexCoords.zw).rgb;
#	endif

//---- Get emission color.
#	if defined(EMISSION_COLOR)
		float3 emission = GammaColor(EmissionColor).rgb;
#	elif defined(EMISSION_TEXTURE)
		float3 emission = GammaTextureRead(EmissionSampler, aIn.EmissionTransparentTexCoords.xy).rgb;
#	endif

//---- Get transparent color and calculate alpha. Note that transparent color (rgb part)
//---- is read for future compatability. COLLADA exports transparent color in RGB_ZERO mode
//---- that should produce colored transparency, but this cannot be done using the standard
//---- pipeline. It requires post-processing effects.
#	if defined(TRANSPARENT_COLOR)
		float4 transparent = TransparentColor;
#	elif defined(TRANSPARENT_TEXTURE)
		float4 transparent = tex2D(TransparentSampler, aIn.EmissionTransparentTexCoords.zw);
#	endif
#	if defined(TRANSPARENT)
#		if defined(ALPHA_ONE)
			alpha = transparent.a * Transparency;
#		elif defined(RGB_ZERO)
			alpha = 1.0f - (((0.212671 * transparent.r) + (0.715160 * transparent.g) + (0.072169 * transparent.b)) * Transparency);
#		endif	
#	endif

//---- Get reflective color and combine with diffuse.
#	if defined(REFLECTIVE_COLOR)
		float3 reflective = GammaColor(ReflectiveColor).rgb;
#	elif defined(REFLECTIVE_TEXTURE)
		float3 reflective = GammaTextureRead(ReflectiveSampler, aIn.ReflectiveTexCoords).rgb;
#	endif
#	if defined(REFLECTIVE_COLOR) || defined(REFLECTIVE_TEXTURE)
#		if defined(DIFFUSE_COLOR) || defined(DIFFUSE_TEXTURE)
			diffuse = lerp(diffuse, reflective, Reflectivity);
#		else
			diffuse = reflective;
#		endif			
#	endif

//---- Calculate output color.
	float3 ret = float3(0, 0, 0);

#	if (defined(DIFFUSE) || defined(REFLECTIVE)) && defined(AMBIENT)
		ret += (diffuse * ambient);
#	endif

#	if defined(EMISSION)
		ret += emission;
#	endif

//---- "Premultiplied alpha - see http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Premultiplied%20alpha]]
#	if defined(TRANSPARENT)
		ret *= alpha;
#	endif

    return float4(ret, alpha);
}


struct fsOut
{
	float4 Diffuse : COLOR0;
	float4 SpecularShininess : COLOR1;
	float4 EyePosition : COLOR2;
	float4 EyeNormal : COLOR3;
};

fsOut FragmentDeferred(vsOutDeferred aIn)
{
//---- Get transparent color and calculate alpha. Note that transparent color (rgb part)
//---- is read for future compatability. COLLADA exports transparent color in RGB_ZERO mode
//---- that should produce colored transparency, but this cannot be done using the standard
//---- pipeline. It requires post-processing effects.
#	if defined(TRANSPARENT_COLOR)
		float4 transparent = TransparentColor;
#	elif defined(TRANSPARENT_TEXTURE)
		float4 transparent = tex2D(TransparentSampler, aIn.TransparentTexCoords);
#	endif
#	if defined(TRANSPARENT)
		float alpha = 1.0f;
		
#		if defined(ALPHA_ONE)
			alpha = transparent.a * Transparency;
#		elif defined(RGB_ZERO)
			alpha = 1.0f - (((0.212671 * transparent.r) + (0.715160 * transparent.g) + (0.072169 * transparent.b)) * Transparency);
#		endif	

		// TODO: fragment deferred is also acting as a base Z pass and this instruction may be hurting HZ performance
		// during this pass.
		clip(alpha - OPAQUE_OF_TRANSPARENCY_F);
#	endif

	fsOut ret;
	
#	if defined(DIFFUSE_COLOR)
		ret.Diffuse = float4(GammaColor(DiffuseColor).rgb, 1);
#	elif defined(DIFFUSE_TEXTURE)
		ret.Diffuse = float4(GammaTextureRead(DiffuseSampler, aIn.DiffuseReflectiveTexCoords.xy).rgb, 1);
#	else
		ret.Diffuse = float4(0, 0, 0, 1);
#	endif

//---- Get reflective color and combine with diffuse.
#	if defined(REFLECTIVE_COLOR)
		float3 reflective = GammaColor(ReflectiveColor).rgb;
#	elif defined(REFLECTIVE_TEXTURE)
		float3 reflective = GammaTextureRead(ReflectiveSampler, aIn.DiffuseReflectiveTexCoords.zw);
#	endif
#	if defined(REFLECTIVE)
#		if defined(DIFFUSE)
			ret.Diffuse = float4(lerp(ret.Diffuse, reflective, Reflectivity), 1);
#		else
			ret.Diffuse = float4(reflective, 1);
#		endif			
#	endif

//---- Get specular color
#	if defined(SPECULAR_COLOR)
		ret.SpecularShininess = float4(GammaColor(SpecularColor).rgb, Shininess);
#	elif defined(SPECULAR_TEXTURE)
		ret.SpecularShininess = float4(GammaTextureRead(SpecularSampler, aIn.SpecularBumpTexCoords.xy).rgb, Shininess);
#	else
		ret.SpecularShininess = float4(0, 0, 0, 0);
#	endif

//---- Get normal
#	if defined(BUMP)
#		error Not yet implemented.
#	else
		ret.EyeNormal = float4(normalize(aIn.Normal.xyz), 1);
#	endif

//---- Get position
	ret.EyePosition = float4(aIn.PositionFragment.xyz, 1);

//---- Premultiplied alpha.
#	if defined(TRANSPARENT)
		ret.Diffuse = float4(ret.Diffuse.rgb * alpha, ret.Diffuse.a);
		ret.SpecularShininess = float4(ret.SpecularShininess.rgb * alpha, ret.SpecularShininess.a);
#	endif

	return ret;
}

float4 Fragment(vsOut aIn, uniform bool abPoint, uniform bool abSpot, uniform bool abShadow, uniform bool abShadowFiltered) : COLOR
{
	float alpha = 1.0f;
	
//---- Get diffuse color.
#	if defined(DIFFUSE_COLOR)
		float3 diffuse = GammaColor(DiffuseColor).rgb;
#	elif defined(DIFFUSE_TEXTURE)
		float3 diffuse = GammaTextureRead(DiffuseSampler, aIn.DiffuseTransparentTexCoords.xy);
#	elif defined(DIFFUSE_VERTEX)
		float3 diffuse = GammaColor(aIn.DiffuseColor).rgb;
#	endif

//---- Get transparent color and calculate alpha. Note that transparent color (rgb part)
//---- is read for future compatability. COLLADA exports transparent color in RGB_ZERO mode
//---- that should produce colored transparency, but this cannot be done using the standard
//---- pipeline. It requires post-processing effects.
#	if defined(TRANSPARENT_COLOR)
		float4 transparent = TransparentColor;
#	elif defined(TRANSPARENT_TEXTURE)
		float4 transparent = tex2D(TransparentSampler, aIn.DiffuseTransparentTexCoords.zw);
#	endif
#	if defined(TRANSPARENT)
#		if defined(ALPHA_ONE)
			alpha = transparent.a * Transparency;
#		elif defined(RGB_ZERO)
			alpha = 1.0f - (((0.212671 * transparent.r) + (0.715160 * transparent.g) + (0.072169 * transparent.b)) * Transparency);
#		endif	
#	endif

//---- Get reflective color and combine with diffuse.
#	if defined(REFLECTIVE_COLOR)
		float3 reflective = GammaColor(ReflectiveColor).rgb;
#	elif defined(REFLECTIVE_TEXTURE)
		float3 reflective = GammaTextureRead(ReflectiveSampler, aIn.ReflectiveSpecularTexCoords.xy);
#	endif
#	if defined(REFLECTIVE)
#		if defined(DIFFUSE)
			diffuse = lerp(diffuse, reflective, Reflectivity);
#		else
			diffuse = reflective;
#		endif			
#	endif

//---- Get specular color
#	if defined(SPECULAR_COLOR)
		float3 specular = GammaColor(SpecularColor).rgb;
#	elif defined(SPECULAR_TEXTURE)
		float3 specular = GammaTextureRead(SpecularSampler, aIn.ReflectiveSpecularTexCoords.zw);
#	endif

//---- Calculate light vectors if necessary.
#	if defined(DIFFUSE) || defined(REFLECTIVE) || defined(SPECULAR)
		float3 lv = normalize(aIn.Light.xyz);

#		if defined(BUMP)
			float4 bump = tex2D(BumpSampler, aIn.BumpTexCoords.xy);
			float3 nv = normalize((2.0 * bump.rgb) - 1.0);
#		else
			float3 nv = normalize(aIn.Normal.xyz);
#		endif
#	endif

//---- Calculate eye vector if necessary and light component vector if necessary.
#	if defined(SPECULAR)
		float3 ev = normalize(aIn.Eye.xyz);
		float ndotl = dot(nv, lv);
		
#		if defined(BLINN)
			float3 hv = normalize(ev + lv);
			float4 l = float4(1, max(ndotl, 0.0f), ndotl > 0.0f ? pow(max(dot(hv, nv), 0.0f), max(Shininess, 1e-3)) : 0.0f, 1);
#		elif defined(PHONG)
			float3 rv = (2.0f * ndotl * nv) - lv;
			float4 l = float4(1, max(ndotl, 0.0f), ndotl > 0.0f ? pow(max(dot(rv, ev), 0.0f), max(Shininess, 1e-3)) : 0.0f, 1); 
#		endif
#	elif defined(DIFFUSE)
		float4 l = float4(1, max(dot(nv, lv), 0.0f), 0, 1);
#	endif

	float3 ret = float3(0, 0, 0);

#	if defined(DIFFUSE) || defined(REFLECTIVE) || defined(SPECULAR)
	//---- Calculate attenuation if spot or point light.
		float distance = 0.0f;
		float att = 1.0f;
		if (abSpot || abPoint)
		{
			distance = length(aIn.Light.xyz);
			att = 1.0f / (LightAttenuation.x + (LightAttenuation.y * distance) + (LightAttenuation.z * distance * distance));
		}
#	endif

//---- Calculate diffuse contribution.
#	if defined(DIFFUSE)
		ret += LightDiffuse * diffuse * l.y;
#	endif

//---- Calculate specular contribution.
#	if defined(SPECULAR)
		ret += LightSpecular * specular * l.z;
#	endif

#	if defined(DIFFUSE) || defined(SPECULAR)
		ret *= att;
#	endif

//---- "Premultiplied alpha - see http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Premultiplied%20alpha]]
#	if defined(TRANSPARENT)
		ret *= alpha;
#	endif

#	if defined(DIFFUSE) || defined(REFLECTIVE) || defined(SPECULAR)
	//---- If a spot light, calculate spot contribution.
		if (abSpot)
		{
			float spotDot = -dot(lv, SpotDirection);
			float spot = pow(max(spotDot, 0.0f), max(SpotFalloffExponent, 1e-3));
			if (spotDot < SpotFalloffCosAngle) { spot = 0.0f; }		
			
			ret *= spot;
		}

	//---- If a shadow casting light, calculate shadow contribution.	
		if (abShadow)
		{
			float pixelDepth = ((distance / ShadowFarDepth) - kShadowDepthBias);
			ret *= Shadow(aIn.ShadowTexCoords, pixelDepth, abShadowFiltered);
		}
#	endif
    
    return float4(ret, alpha);
}

#define _COMMON_RENDER_STATES	\
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA; \
		FillMode = Solid;

#define _COMMON_TRANSPARENT_RENDER_STATES_LIT \
		AlphaBlendEnable = true; \
		DestBlend = One; \
		SrcBlend = One;

#define _COMMON_OPAQUE_RENDER_STATES_LIT \
		AlphaBlendEnable = true; \
		DestBlend = One; \
		SrcBlend = One;
		
// Base technique - unlit base pass for every object in the view frustum
#define TECHNIQUE_NAME siat_RenderBase
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES \
		AlphaBlendEnable = true; \
		DestBlend = InvSrcAlpha; \
		SrcBlend = One;
#define COMMON_OPAQUE_RENDER_STATES \
		AlphaBlendEnable = false;
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_2_0 VertexBase(); \
		PixelShader = compile ps_2_0  FragmentBase();
#include "_collada_effect_technique.h"

// Deferred lighting technique
#if !defined(TRANSPARENT) || (defined(TRANSPARENT_TEXTURE) && defined(TRANSPARENT_TEXTURE_1_BIT))
	technique siat_RenderDeferred
	{
		pass
		{
			_COMMON_RENDER_STATES
			AlphaBlendEnable = false;
			AlphaTestEnable = false;
			CullMode = BACK_FACE_CULLING;
			ZWriteEnable = true;
		
			VertexShader = compile vs_2_0 VertexDeferred();
			PixelShader = compile ps_2_0 FragmentDeferred();
		}
	}
#endif

// Directional light technique - applies a directional light.
#define TECHNIQUE_NAME siat_RenderDirectionalLight
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES _COMMON_TRANSPARENT_RENDER_STATES_LIT
#define COMMON_OPAQUE_RENDER_STATES _COMMON_OPAQUE_RENDER_STATES_LIT
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_2_0 Vertex(true, false, false, false); \
		PixelShader = compile ps_2_0 Fragment(false, false, false, false);
#include "_collada_effect_technique.h"

// Point light technique - applies a point light.
#define TECHNIQUE_NAME siat_RenderPointLight
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES _COMMON_TRANSPARENT_RENDER_STATES_LIT
#define COMMON_OPAQUE_RENDER_STATES _COMMON_OPAQUE_RENDER_STATES_LIT
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_2_0 Vertex(false, true, false, false); \
		PixelShader = compile ps_2_0 Fragment(true, false, false, false);
#include "_collada_effect_technique.h"

// Spot light technique - applies a spot light.
#define TECHNIQUE_NAME siat_RenderSpotLight
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES _COMMON_TRANSPARENT_RENDER_STATES_LIT
#define COMMON_OPAQUE_RENDER_STATES _COMMON_OPAQUE_RENDER_STATES_LIT
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_2_0 Vertex(false, false, true, false); \
		PixelShader = compile ps_2_0 Fragment(false, true, false, false);
#include "_collada_effect_technique.h"

// Spot light with shadow technique - applies a shadowed spot light. Unfiltered edge.
#define TECHNIQUE_NAME siat_RenderSpotLightShadow_Unfiltered
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES _COMMON_TRANSPARENT_RENDER_STATES_LIT
#define COMMON_OPAQUE_RENDER_STATES _COMMON_OPAQUE_RENDER_STATES_LIT
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_2_0 Vertex(false, false, true, true); \
		PixelShader = compile ps_2_0 Fragment(false, true, true, false);
#include "_collada_effect_technique.h"

// Spot light with shadow technique - applies a shadowed spot light. Filters the edge with a box filter.
#define TECHNIQUE_NAME siat_RenderSpotLightShadow_Filtered
#define COMMON_RENDER_STATES _COMMON_RENDER_STATES
#define COMMON_TRANSPARENT_RENDER_STATES _COMMON_TRANSPARENT_RENDER_STATES_LIT
#define COMMON_OPAQUE_RENDER_STATES _COMMON_OPAQUE_RENDER_STATES_LIT
#define COMMON_SHADER_DEFINE \
		VertexShader = compile vs_3_0 Vertex(false, false, true, true); \
		PixelShader = compile ps_3_0 Fragment(false, true, true, true);
#include "_collada_effect_technique.h"

// Special technique used for picking. Renders a solid color. If material is transparent,
// pixel is only rendered if alpha is above a certain threshold.
technique siat_RenderPicking
{
	pass
	{
#if defined(TRANSPARENT)
#	if !(defined(TRANSPARENT_TEXTURE) && defined(TRANSPARENT_TEXTURE_1_BIT))		
		CullMode = None;
#	else
		CullMode = BACK_FACE_CULLING;
#	endif
#else
		CullMode = BACK_FACE_CULLING;
#endif
	
		AlphaTestEnable = false;
		AlphaBlendEnable = false;
		ColorWriteEnable = RED|GREEN|BLUE|ALPHA;
		FillMode = Solid;
		ZWriteEnable = true;
	
		VertexShader = compile vs_2_0 VertexPicking();
		PixelShader = compile ps_2_0 FragmentPicking();
	}
}