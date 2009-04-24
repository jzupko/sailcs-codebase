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

#define DEPTH_DEBUG

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;

namespace siat.render
{
    public sealed class DepthRasterizer
    {
        #region Private members
        private int mDataSize = 0;
        private float[] mData = new float[0];

        private int mWidth = 0;
        private int mHeight = 0;

        private delegate bool DepthCompare(float aNew, float aOld);
        private static readonly DepthCompare[] kDepthComparers = new DepthCompare[]
            {
                null, // No value for 0
                delegate(float aNew, float aOld) { return false; }, // Never - 1
                delegate(float aNew, float aOld) { return (aNew < aOld); }, // Less - 2
                delegate(float aNew, float aOld) { return (aNew == aOld); }, // Equal - 3
                delegate(float aNew, float aOld) { return (aNew <= aOld); }, // LessEqual - 4
                delegate(float aNew, float aOld) { return (aNew > aOld); }, // Greater - 5
                delegate(float aNew, float aOld) { return (aNew != aOld); }, // NotEqual - 6
                delegate(float aNew, float aOld) { return (aNew >= aOld); }, // GreaterEqual - 7
                delegate(float aNew, float aOld) { return true; } // Always = 8
            };

        private bool mbClipping = true;
        private CullMode mCullMode = CullMode.None;
        private DepthCompare mDepthComparer = kDepthComparers[(int)CompareFunction.LessEqual];
        private CompareFunction mDepthFunction = CompareFunction.LessEqual;
        private bool mbZbuffer = true;

        private Matrix mProjectionMatrix = Matrix.Identity;
        private Matrix mViewMatrix = Matrix.Identity;
        private Matrix mWorldMatrix = Matrix.Identity;

        private Matrix mWVP = Matrix.Identity;

        private List<Vector3> mRbuffer = new List<Vector3>();
        private List<Vector3> mRbuffer2 = new List<Vector3>();

        #region Clipping
        private const float kXMin = -1;
        private const float kXMax = 1;
        private const float kYMin = -1;
        private const float kYMax = 1;
        private const float kZMin = 0;
        private const float kZMax = 1;

        private const float kXFactor = (kXMax - kXMin);
        private const float kYFactor = (kYMax - kYMin);
        private const float kZFactor = (kZMax - kZMin);
        private const float kInvXFactor = 1.0f / (kXMax - kXMin);
        private const float kInvYFactor = 1.0f / (kYMax - kYMin);
        private const float kInvZFactor = 1.0f / (kZMax - kZMin);

        private const float kXMinD = 1;
        private const float kXMaxD = 1;
        private const float kYMinD = 1;
        private const float kYMaxD = 1;
        private const float kZMinD = 0;
        private const float kZMaxD = 1;

        private delegate PlaneIntersectionType ClipDelegate(Vector3 v);
        private delegate Vector3 IntersectionDelegate(Vector3 v0, Vector3 v1);

        private const int kDelegateCount = 6;
        #region Clip delegates
        private static readonly ClipDelegate[] kClipDelegates = new ClipDelegate[]
            {
                delegate(Vector3 v) { return (v.X < kXMin) ? PlaneIntersectionType.Back : ((v.X > kXMin) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); },
                delegate(Vector3 v) { return (v.X > kXMax) ? PlaneIntersectionType.Back : ((v.X < kXMax) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); },
                delegate(Vector3 v) { return (v.Y < kYMin) ? PlaneIntersectionType.Back : ((v.Y > kYMin) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); },
                delegate(Vector3 v) { return (v.Y > kYMax) ? PlaneIntersectionType.Back : ((v.Y < kYMax) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); },
                delegate(Vector3 v) { return (v.Z < kZMin) ? PlaneIntersectionType.Back : ((v.Z > kZMin) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); },
                delegate(Vector3 v) { return (v.Z > kZMax) ? PlaneIntersectionType.Back : ((v.Z < kZMax) ? PlaneIntersectionType.Front : PlaneIntersectionType.Intersecting); }
            };
        #endregion

        #region Intersection delegates
        private static readonly IntersectionDelegate[] kIntersectionDelegates = new IntersectionDelegate[]
            {
                // Left
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = diff.X; // Xdiff dot N
                    float t = -((v0.X + kXMinD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },
                // Right
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = -diff.X; // Xdiff dot N
                    float t = -((-v0.X + kXMaxD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },
                // Bottom
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = diff.Y; // Ydiff dot N
                    float t = -((v0.Y + kYMinD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },
                // Top
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = -diff.Y; // Ydiff dot N
                    float t = -((-v0.Y + kYMaxD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },
                // Near
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = diff.Z; // Zdiff dot N
                    float t = -((v0.Z + kZMinD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },
                // Far
                delegate(Vector3 v0, Vector3 v1)
                {
                    Vector3 diff = (v1 - v0);
                    float a = -diff.Z; // Zdiff dot N
                    float t = -((-v0.Z + kZMaxD) / a);

                    Vector3 ret = v0 + (diff * t);
                    return ret;
                },

            };
        #endregion

        private PlaneIntersectionType[] mClipBuffer = new PlaneIntersectionType[0];
        private void _Clip(ClipDelegate dC, IntersectionDelegate dI)
        {
            int positive = 0;
            int negative = 0;

            int count = mRbuffer.Count;

            if (mClipBuffer.Length != count) { Array.Resize(ref mClipBuffer, count); }
            for (int i = 0; i < count; i++)
            {
                PlaneIntersectionType inter = dC(mRbuffer[i]);
                if (inter == PlaneIntersectionType.Front) { positive++; }
                else if (inter == PlaneIntersectionType.Back) { negative++; }

                mClipBuffer[i] = inter;
            }

            if (negative == 0) { return; }
            else if (positive == 0) { mRbuffer.Clear(); return; }

            mRbuffer2.Clear();

            int Si = (count - 1);

            for (int Pi = 0; Pi < count; Pi++)
            {
                PlaneIntersectionType P = mClipBuffer[Pi];
                PlaneIntersectionType S = mClipBuffer[Si];

                if (P == PlaneIntersectionType.Front)
                {
                    if (S == PlaneIntersectionType.Front)
                    {
                        mRbuffer2.Add(mRbuffer[Pi]);
                    }
                    else
                    {
                        mRbuffer2.Add(dI(mRbuffer[Si], mRbuffer[Pi]));
                        mRbuffer2.Add(mRbuffer[Pi]);
                    }
                }
                else if (S == PlaneIntersectionType.Front)
                {
                    mRbuffer2.Add(dI(mRbuffer[Pi], mRbuffer[Si]));
                }

                Si = Pi;
            }

            Utilities.Swap(ref mRbuffer, ref mRbuffer2);
        }

        private void _Clip()
        {
            for (int i = 0; i < kDelegateCount; i++)
            {
                if (mRbuffer.Count > 0)
                {
                    _Clip(kClipDelegates[i], kIntersectionDelegates[i]);
                }
            }
        }
        #endregion

        #region Culling
        private bool _Cull(Vector4 pp0, Vector4 pp1, Vector4 pp2)
        {
            if (mCullMode != CullMode.None)
            {
#if SIAT_DEFAULT_CLOCKWISE_WINDING
                Vector2 u = new Vector2(pp2.X - pp0.X, pp2.Y - pp0.Y);
                Vector2 v = new Vector2(pp1.X - pp0.X, pp1.Y - pp0.Y);
#elif SIAT_DEFAULT_COUNTER_CLOCKWISE_WINDING
                Vector2 u = new Vector2(pp1.X - pp0.X, pp1.Y - pp0.Y);
                Vector2 v = new Vector2(pp2.X - pp0.X, pp2.Y - pp0.Y);
#endif

                float d = Utilities.Cross(u, v);

                if (mCullMode == Utilities.kFrontFaceCulling && d < 0.0f) { return true; }
                else if (mCullMode == Utilities.kBackFaceCulling && d > 0.0f) { return true; }
            }

            return false;
        }
        #endregion

        #region Rasterizing
        private int _Index(int x, int y) { return (y * mWidth) + x; }

        private struct ScreenCoords
        {
            public static readonly ScreenCoords kMax = new ScreenCoords(float.MaxValue, float.MaxValue, float.MaxValue);
            public static readonly ScreenCoords kMin = new ScreenCoords(float.MinValue, float.MinValue, float.MinValue);

            public ScreenCoords(float x, float y, float z)
            {
                iX = (int)x;
                iY = (int)y;
                fX = x;
                fY = y;
                Z = z;
            }

            public static ScreenCoords From(Vector3 v, float aWidth, float aHeight)
            {
                float x = (float)Math.Floor((v.X - kXMin) * kInvXFactor * (aWidth - 1.0f));
                float y = (float)Math.Floor((1.0f - ((v.Y - kYMin) * kInvYFactor)) * (aHeight - 1.0f));

                x = Utilities.Max(x, 0.0f);
                y = Utilities.Max(y, 0.0f);

                ScreenCoords ret = new ScreenCoords(x, y, v.Z);

                return ret;
            }

            public static void Intersect(ScreenCoords a, Slopes aM, int y, out int x, out float z)
            {
                float fY = (float)y;
                Debug.Assert(!Utilities.AboutEqual(fY, a.fY));
                float diff = (fY - a.fY);

                x = (int)(a.fX + (aM.MX * diff));
                z = (a.Z + (aM.MZ * diff));
            }

            public static ScreenCoords Max(ScreenCoords a, ScreenCoords b)
            {
                ScreenCoords ret = new ScreenCoords(Utilities.Max(a.fX, b.fX), Utilities.Max(a.fY, b.fY), Utilities.Max(a.Z, b.Z));

                return ret;
            }

            public static ScreenCoords Min(ScreenCoords a, ScreenCoords b)
            {
                ScreenCoords ret = new ScreenCoords(Utilities.Min(a.fX, b.fX), Utilities.Min(a.fY, b.fY), Utilities.Min(a.Z, b.Z));

                return ret;
            }

            public static Slopes Slopes(ScreenCoords a, ScreenCoords b)
            {
                Debug.Assert(!Utilities.AboutEqual(b.fY, a.fY));

                float f = 1.0f / (b.fY - a.fY);

                float x = (b.fX - a.fX) * f;
                float z = (b.Z - a.Z) * f;

                Slopes ret = new Slopes(x, z);

                return ret;
            }

            public readonly int iX;
            public readonly int iY;
            public readonly float fX;
            public readonly float fY;
            public readonly float Z;
        }

        private struct Slopes
        {
            public Slopes(float aMX, float aMZ)
            {
                MX = aMX;
                MZ = aMZ;
            }

            public static Slopes operator -(Slopes a)
            {
                return new Slopes(-a.MX, -a.MZ);
            }

            public readonly float MX;
            public readonly float MZ;
        }

        private ScreenCoords[] mRasterBuffer = new ScreenCoords[0];
        private Slopes[] mSlopeBuffer = new Slopes[0];
        private void _Rasterize(DepthCompare d)
        {
            ScreenCoords min = ScreenCoords.kMax;
            ScreenCoords max = ScreenCoords.kMin;
            int count = mRbuffer.Count;

            #region Initialize buffers and gather min/max
            {
                if (mRasterBuffer.Length != count)
                {
                    Array.Resize(ref mRasterBuffer, count);
                    Array.Resize(ref mSlopeBuffer, count);
                }

                for (int i = 0; i < count; i++)
                {
                    ScreenCoords e = ScreenCoords.From(mRbuffer[i], mWidth, mHeight);
                    mRasterBuffer[i] = e;

                    min = ScreenCoords.Min(min, e);
                    max = ScreenCoords.Max(max, e);
                }

                int prev = (count - 1);
                for (int i = 0; i < count; i++)
                {
                    if (mRasterBuffer[prev].iY != mRasterBuffer[i].iY)
                    {
                        mSlopeBuffer[i] = ScreenCoords.Slopes(mRasterBuffer[prev], mRasterBuffer[i]);
                    }
                    else
                    {
                        mSlopeBuffer[i] = new Slopes(0.0f, 0.0f);
                    }

                    prev = i;
                }
            }
            #endregion

            #region Draw
            int y0 = (int)min.iY;
            int y1 = (int)max.iY;

            if (y0 >= 0 && y0 < mHeight &&
                y1 >= 0 && y1 < mHeight &&
                y1 > y0)
            {
                for (int y = y0; y <= y1; y++)
                {
                    #region Find x0, x1
                    int x0 = int.MaxValue;
                    int x1 = int.MinValue;
                    float x0Z = 0.0f;
                    float x1Z = 0.0f;

                    int prev = (count - 1);
                    for (int i = 0; i < count; i++)
                    {
                        if (mRasterBuffer[prev].iY <= y && mRasterBuffer[i].iY > y)
                        {
                            int v;
                            float z;
                            ScreenCoords.Intersect(mRasterBuffer[i], mSlopeBuffer[i], y, out v, out z);
                            if (v < x0) { x0 = v; x0Z = z; }
                            if (v > x1) { x1 = v; x1Z = z; }
                        }
                        else if (mRasterBuffer[prev].iY > y && mRasterBuffer[i].iY <= y)
                        {
                            int v;
                            float z;
                            ScreenCoords.Intersect(mRasterBuffer[prev], mSlopeBuffer[i], y, out v, out z);
                            if (v < x0) { x0 = v; x0Z = z; }
                            if (v > x1) { x1 = v; x1Z = z; }
                        }

                        prev = i;
                    }
                    #endregion

                    #region Draw
                    if (x0 >= 0 && x0 < mWidth && x1 >= 0 && x1 < mWidth && x1 > x0)
                    {
                        float f = 1.0f / ((float)(x1 - x0));
                        int iStart = _Index(x0, y);
                        int iEnd = _Index(x1, y);
                        for (int i = iStart; i <= iEnd; i++)
                        {
                            float t = (float)(i - iStart) * f;
                            float z = MathHelper.Lerp(x0Z, x1Z, t);

                            if (d(z, mData[i]))
                            {
                                mData[i] = z;
                            }
                        }
                    }
                    #endregion
                }
            }
            #endregion
        }

        private void _RasterizeZBuffer()
        {
            _Rasterize(mDepthComparer);
        }

        private void _RasterizeNoZBuffer()
        {
            _Rasterize(kDepthComparers[(int)CompareFunction.Always]);
        }
        #endregion

        private void _UpdateWVP()
        {
            mWVP = (mWorldMatrix * mViewMatrix * mProjectionMatrix);
        }
        #endregion

        public DepthRasterizer() : this(0, 0) { }
        public DepthRasterizer(int aWidth, int aHeight) { Resize(aWidth, aHeight); }

#if DEPTH_DEBUG
        private float mDHighPass = 0.98f;
        public float DepthVisualizeHighPass { get { return mDHighPass; } set { mDHighPass = value; } }
#endif

        public bool EnableClipping { get { return mbClipping; } set { mbClipping = value; } }
        public bool EnableZBuffer { get { return mbZbuffer; } set { mbZbuffer = value; } }
        public CullMode CullMode { get { return mCullMode; } set { mCullMode = value; } }
        public CompareFunction DepthFunction
        {
            get { return mDepthFunction; }
            set
            {
                mDepthFunction = value;
                mDepthComparer = kDepthComparers[(int)value];
            }
        }

        public Matrix ProjectionMatrix { get { return mProjectionMatrix; } set { mProjectionMatrix = value; _UpdateWVP(); } }
        public Matrix ViewMatrix { get { return mViewMatrix; } set { mViewMatrix = value; _UpdateWVP(); } }
        public Matrix WorldMatrix { get { return mWorldMatrix; } set { mWorldMatrix = value; _UpdateWVP(); } }

        public void Clear(float aDepth)
        {
            for (int i = 0; i < mDataSize; i++) { mData[i] = aDepth; }
        }

        public void Rasterize(Triangle aTriangle)
        {
            #region Project
            Vector4 pp0 = Vector4.Transform(new Vector4(aTriangle.P0, 1), mWVP);
            Vector4 pp1 = Vector4.Transform(new Vector4(aTriangle.P1, 1), mWVP);
            Vector4 pp2 = Vector4.Transform(new Vector4(aTriangle.P2, 1), mWVP);

            if (Utilities.AboutZero(pp0.W)) { return; }
            if (Utilities.AboutZero(pp1.W)) { return; }
            if (Utilities.AboutZero(pp2.W)) { return; }

            pp0 /= pp0.W;
            pp1 /= pp1.W;
            pp2 /= pp2.W;
            #endregion

            #region Cull
            if (_Cull(pp0, pp1, pp2)) { return; }
            #endregion

            mRbuffer.Clear();
            mRbuffer.Add(Utilities.ToVector3(pp0));
            mRbuffer.Add(Utilities.ToVector3(pp1));
            mRbuffer.Add(Utilities.ToVector3(pp2));

            #region Clip
            if (mbClipping)
            {
                _Clip();
                if (mRbuffer.Count == 0) { return; }
            }
            #endregion

            #region Rasterize
            if (mbZbuffer) { _RasterizeZBuffer(); }
            else { _RasterizeNoZBuffer(); }
            #endregion
        }

        public void Resize(int aWidth, int aHeight)
        {
            if (mWidth != aWidth || mHeight != aHeight)
            {
                mWidth = aWidth;
                mHeight = aHeight;
                mDataSize = (mWidth * mHeight);

                mData = new float[mDataSize];
            }
        }

        public void Get(Texture2D aTexture)
        {
            if (aTexture.Width != mWidth) { throw new ArgumentException("Width of aTexture is not equal to rasterizer width."); }
            if (aTexture.Height != mHeight) { throw new ArgumentException("Height of aTexture is not equal to rasterizer height."); }

            if (aTexture.Format == SurfaceFormat.Color)
            {
                Microsoft.Xna.Framework.Graphics.Color[] data = new Microsoft.Xna.Framework.Graphics.Color[mDataSize];
                for (int i = 0; i < mDataSize; i++)
                {
#if DEPTH_DEBUG
                    byte v = (byte)(Utilities.Clamp((mData[i] - mDHighPass) / (1.0f - mDHighPass), 0.0f, 1.0f) * 255.0f);
#else
                    byte v = (byte)(mData[i] * 255.0);
#endif
                    data[i] = new Microsoft.Xna.Framework.Graphics.Color(v, v, v, 255);
                }

                aTexture.SetData<Microsoft.Xna.Framework.Graphics.Color>(data);
            }
            else if (aTexture.Format == SurfaceFormat.Single)
            {
                aTexture.SetData<float>(mData);
            }
        }

        public void Save(string aFilename)
        {
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(mWidth, mHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                for (int y = 0; y < mHeight; y++)
                {
                    for (int x = 0; x < mWidth; x++)
                    {
                        int i = _Index(x, y);
                        int v = (int)(mData[i] * 255.0f);

                        System.Drawing.Color color = System.Drawing.Color.FromArgb(255, v, v, v);
                        bitmap.SetPixel(x, y, color);
                    }
                }

                bitmap.Save(aFilename);
            }
        }
    }
}
