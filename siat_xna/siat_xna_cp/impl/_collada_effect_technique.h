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

technique TECHNIQUE_NAME
{
#if defined(TRANSPARENT_TEXTURE)
	pass Pass0
	{
		COMMON_RENDER_STATES
		COMMON_OPAQUE_RENDER_STATES
		COMMON_SHADER_DEFINE
		
		AlphaTestEnable = true;
		AlphaFunc = GreaterEqual;
		AlphaRef = OPAQUE_OF_TRANSPARENCY;
#		if defined(TRANSPARENT_TEXTURE_1_BIT)
			CullMode = BACK_FACE_CULLING;
#		else
			CullMode = None;
#		endif
		ZWriteEnable = true;
	}

#	if !defined(TRANSPARENT_TEXTURE_1_BIT)
		pass Pass1
		{
			COMMON_RENDER_STATES
			COMMON_TRANSPARENT_RENDER_STATES
			COMMON_SHADER_DEFINE
			
			AlphaTestEnable = true;
			AlphaFunc = Less;
			AlphaRef = OPAQUE_OF_TRANSPARENCY;
			CullMode = FRONT_FACE_CULLING;
			ZWriteEnable = false;
		}
			
		pass Pass2
		{
			COMMON_RENDER_STATES
			COMMON_TRANSPARENT_RENDER_STATES
			COMMON_SHADER_DEFINE
			
			AlphaTestEnable = true;
			AlphaFunc = Less;
			AlphaRef = OPAQUE_OF_TRANSPARENCY;
			CullMode = BACK_FACE_CULLING;
			ZWriteEnable = false;
		}
#	endif

#elif defined (TRANSPARENT)
	pass Pass0
	{
		COMMON_RENDER_STATES
		COMMON_TRANSPARENT_RENDER_STATES
		COMMON_SHADER_DEFINE
		
		AlphaTestEnable = false;
		CullMode = FRONT_FACE_CULLING;
		ZWriteEnable = false;
	}
	
	pass Pass1
	{
		COMMON_RENDER_STATES
		COMMON_TRANSPARENT_RENDER_STATES
		COMMON_SHADER_DEFINE
		
		AlphaTestEnable = false;
		CullMode = BACK_FACE_CULLING;
		ZWriteEnable = false;
	}
#else
	pass Pass0
	{
		COMMON_RENDER_STATES
		COMMON_OPAQUE_RENDER_STATES
		COMMON_SHADER_DEFINE
		
		AlphaTestEnable = false;
		CullMode = BACK_FACE_CULLING;
		ZWriteEnable = true;
	}
#endif

#undef COMMON_SHADER_DEFINE
#undef COMMON_OPAQUE_RENDER_STATES
#undef COMMON_TRANSPARENT_RENDER_STATES
#undef COMMON_RENDER_STATES
#undef TECHNIQUE_NAME
}
