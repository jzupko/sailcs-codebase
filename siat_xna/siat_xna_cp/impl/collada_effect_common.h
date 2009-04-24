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

//-----------------------------------------------------------------------------
// static constants
//-----------------------------------------------------------------------------
static const int kSkinningMatricesCount = 72;
static const int kSkinningMatricesSize = kSkinningMatricesCount * 3;

static const float kLooseTolerance = 1e-3;

// this will be client specified eventually to allow for different size shadow maps.
static const int kShadowDimension = 512;
static const float kShadowDelta = 1.0f / ((float)kShadowDimension);

static const float kShadowSlopeBias = 0.25;
static const float kShadowDepthBias = 3.81e-4;

// This is the alpha value that a transparent alpha must be above to be considered
// "solid" in the picking technique. Any value lower than this threshold will be
// considered completely transparent and will cause the pick to go through the object.
#define PICK_ALPHA_THRESHOLD 64

// This is the alpha value that will be considered opaque. Any alpha equal to or
// greater than this value will be rendered with zwrites enabled and alpha blending
// turned off.
#define OPAQUE_OF_TRANSPARENCY 127
#define OPAQUE_OF_TRANSPARENCY_F (127.0 / 255.0)

#define BACK_FACE_CULLING Ccw
#define FRONT_FACE_CULLING Cw

// Objects with transparent textures are treated as 1-bit alpha - alpha is either off/on
// and these objects are rendered as opaque objects with masking.
#define TRANSPARENT_TEXTURE_1_BIT 1
