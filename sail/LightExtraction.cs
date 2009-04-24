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
using siat;

namespace sail
{

    public struct ImageIlluminationMetrics
    {
        public const float kMaxIntensityMin = 0.0f;
        public const float kMaxIntensityMax = 1.0f;
        public const float kEntropyMin = 0.0f;
        public const float kEntropyMax = 1.0f;
        public const float kRollMin = 0.0f;
        public const float kRollMax = 2.0f;
        public const float kYawMin = -1.0f;
        public const float kYawMax = 1.0f;

        public const float kHalfRollMax = 0.5f * kRollMax;

        public float MaxIntensity; // 0...1
        public float Entropy; // 0...1
        public float Roll; // 0...2, 1:1 correlation with 0...360, set to 0...1 so a separation of 180 has the same weight as a 0...1 separation for other terms.
        public float Yaw; // -1...1

        public static ImageIlluminationMetrics operator -(ImageIlluminationMetrics a, ImageIlluminationMetrics b)
        {
            ImageIlluminationMetrics ret;
            ret.MaxIntensity = a.MaxIntensity - b.MaxIntensity;
            ret.Entropy = a.Entropy - b.Entropy;
            ret.Yaw = a.Yaw - b.Yaw;

            float rollA = a.Roll;
            float rollB = b.Roll;

            while (rollA - rollB > kHalfRollMax) { rollA -= kRollMax; }
            while (rollB - rollA > kHalfRollMax) { rollB -= kRollMax; }

            ret.Roll = rollA - rollB;

            return ret;
        }

        public static float Dot(ref ImageIlluminationMetrics a, ref ImageIlluminationMetrics b)
        {
            float ret =
                (a.MaxIntensity * b.MaxIntensity) +
                (a.Entropy * b.Entropy) +
                (a.Roll * b.Roll) +
                (a.Yaw * b.Yaw);

            return ret;
        }

        public static ImageIlluminationMetrics Lerp(ref ImageIlluminationMetrics a, ref ImageIlluminationMetrics b, float aWeightOfB)
        {
            ImageIlluminationMetrics ret;
            ret.MaxIntensity = MathHelper.Lerp(a.MaxIntensity, b.MaxIntensity, aWeightOfB);
            ret.Entropy = MathHelper.Lerp(a.Entropy, b.Entropy, aWeightOfB);
            ret.Yaw = MathHelper.Lerp(a.Yaw, b.Yaw, aWeightOfB);

            float rollA = a.Roll;
            float rollB = b.Roll;

            while (rollA - rollB > kHalfRollMax) { rollA -= kRollMax; }
            while (rollB - rollA > kHalfRollMax) { rollB -= kRollMax; }

            ret.Roll = MathHelper.Lerp(rollA, rollB, aWeightOfB);

            while (ret.Roll > kRollMax) { ret.Roll -= kRollMax; }
            while (ret.Roll < kRollMin) { ret.Roll += kRollMax; }

            return ret;
        }
    }

    public class ImageData
    {
        public static readonly Color kMaskColor = Color.Magenta;

        #region Private members
        private byte[] mData;
        private int mWidth;
        private int mHeight;
        
        private bool[] mMask;
        private int mCount;
        
        private int mX0;
        private int mY0;
        private int mX1;
        private int mY1;

        private static bool _PixelIsValid(SurfaceFormat aFormat, byte[] p, int index)
        {
            Color color = Utilities.GetPixel(aFormat, p, index);

            return !(color.R == kMaskColor.R &&
                     color.G == kMaskColor.G &&
                     color.B == kMaskColor.B);
        }
        #endregion

        public ImageData(int aWidth, int aHeight, SurfaceFormat aFormat, byte[] aData)
        {
            mWidth = aHeight;
            mHeight = aHeight;
            mMask = new bool[mWidth * mHeight];
            mCount = 0;
            mX0 = mWidth;
            mY0 = mHeight;
            mX1 = 0;
            mY1 = 0;

            int stride = Utilities.GetStride(aFormat);
            int pitch  = stride * mWidth;
            byte[] p = null;

            // This is necessary because all the extraction functions in this file assumes byte ordering 
            // from bottom-left to top-right, like OpenGL, but XNA will read-in from top-left, to bottom-right.
            Utilities.FlipVertical(aWidth, aHeight, aFormat, aData, out p);
            
            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    int maskIndex = (y * mWidth) + x;
                    int imageIndex = (y * pitch) + (x * stride);

                    if (_PixelIsValid(aFormat, p, imageIndex))
                    {
                        mMask[maskIndex] = true;
                        mX0 = Utilities.Min(mX0, x);
                        mY0 = Utilities.Min(mY0, y);
                        mX1 = Utilities.Max(mX1, x);
                        mY1 = Utilities.Max(mY1, y);
                        mCount++;
                    }
                    else
                    {
                        mMask[maskIndex] = false;
                    }
                }
            }

            GaussianImageSmooth.Calculate(mX0, mY0, mX1, mY1, mWidth, mHeight, aFormat, p);
            Utilities.ConvertToGrayscale(aWidth, aHeight, aFormat, p, out mData);
        }
        
        public bool IsValid(int x, int y)
        {
            int kIndex = (y * mWidth) + x;
            
            return mMask[kIndex];
        }

        public int Count { get { return mCount; } }
        public int Height { get { return mHeight; } }
        public byte[] Image { get { return mData; } }
        public int X0 { get { return mX0; } }
        public int Y0 { get { return mY0; } }
        public int X1 { get { return mX1; } }
        public int Y1 { get { return mY1; } }
        public int Width { get { return mWidth; } }

        public void Save(string aFilename)
        {
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(mWidth, mHeight,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                for (int i = 0; i < mWidth; i++)
                {
                    for (int j = 0; j < mHeight; j++)
                    {
                        int index = (mWidth * j) + i;
                        System.Drawing.Color color = System.Drawing.Color.FromArgb(255, mData[index], mData[index], mData[index]);
                        bitmap.SetPixel(i, j, color);
                    }
                }

                bitmap.Save(aFilename);
            }
        }
    }

    public struct LightExtractorImage
    {
        public int Width;
        public int Height;
        public SurfaceFormat Format;
        public byte[] Data;
    }

    public static class LightingExtractor
    {
        public const int kMinimumDataSize = 9;
        public const int kEntropyBinCount = 30;
        public const float kBinCountFactor = (float)(kEntropyBinCount - 1);
        public const int kWindowSize = 3;
        public const int kWindowHalfSize = kWindowSize / 2;
        public const float kPixelFactor = (float)(1.0 / 255.0);
        public const float kMaxRoll = 360.0f;
        public const float kHalfMaxRoll = kMaxRoll * 0.5f;
        public const float kBestProbPerBin = (float)(1.0 / ((double)kEntropyBinCount));

        public static readonly float kMaxEntropy = -(float)(((float)kEntropyBinCount) * (kBestProbPerBin * Math.Log(kBestProbPerBin)));

        #region Private members
        private static float _CalculateEntropy(ImageData aData)
        {
            float binFactor = 1.0f / (float)aData.Count;
            byte[] p = aData.Image;
            int width = aData.Width;
            int height = aData.Height;
            int size = width * height;
            int x0 = aData.X0;
            int y0 = aData.Y0;
            int x1 = aData.X1;
            int y1 = aData.Y1;
                    
            int[] bins = new int[kEntropyBinCount];
            Array.Clear(bins, 0, bins.Length);
            
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (aData.IsValid(x, y))
                    {
                        int index = (y * width) + x;
                        byte value = p[index];
                        int binIndex = (int)((((float)value) / 255.0f) * ((float)(kEntropyBinCount - 1)));
                        bins[binIndex]++;
                    }
                }
            }

            float entropy = 0.0f;
            for (int i = 0; i < kEntropyBinCount; i++)
            {
                float prob = ((float)bins[i]) * binFactor;
                
                if (!Utilities.AboutZero(prob))
                {
                    entropy += prob * (float)Math.Log(prob);
                }
            }
            
            return (-entropy) / kMaxEntropy;
        }

        private static float _CalculateRoll(ImageData aData)
        {
            byte[] p = aData.Image;
            int width = aData.Width;
            int height = aData.Height;
            int size = width * height;
            int x0 = aData.X0;
            int y0 = aData.Y0;
            int x1 = aData.X1;
            int y1 = aData.Y1;
            
            int count = 0;
            float  dirX  = 0.0f;
            float  dirY  = 0.0f;
            
            for (int y = y0 + kWindowHalfSize; y <= y1 - kWindowHalfSize; y++)
            {
                for (int x = x0 + kWindowHalfSize; x <= x1 - kWindowHalfSize; x++)
                {
                    int centerMass  = 0;
                    int centerX     = 0;
                    int centerY     = 0;
                    int brightMass  = 0;
                    int brightX     = 0;
                    int brightY     = 0;
                    
                    for (int j = y - kWindowHalfSize; j <= y + kWindowHalfSize; j++)
                    {
                        for (int i = x - kWindowHalfSize; i <= x + kWindowHalfSize; i++)
                        {
                            if (aData.IsValid(i, j))
                            {
                                int kIndex = (j * width) + i;
                                
                                int val = p[kIndex];
                                    
                                centerMass += 255 - val;
                                centerX    += i * (255 - val);
                                centerY    += j * (255 - val);
                                 
                                brightMass += val;
                                brightX    += i * val;
                                brightY    += j * val;
                            }
                        }
                    }

                    if (centerMass > 0 && brightMass > 0)
                    {
                        float fX = ((float)brightX / (float)brightMass) - ((float)centerX / (float)centerMass);
                        float fY = ((float)brightY / (float)brightMass) - ((float)centerY / (float)centerMass);
                     
                        dirX  = dirX + Math.Sign(fX);
                        dirY  = dirY + Math.Sign(fY);
                        count = count + 1;
                    }
                 }
            }

            if (count > 0)
            {
                dirX = dirX / (float)count;
                dirY = dirY / (float)count;
                
                if (Utilities.AboutZero(dirX))
                {
                    dirX = 0.0f;
                }
                if (Utilities.AboutZero(dirY))
                {
                    dirY = 0.0f;
                }
            }

            Vector2 v = new Vector2(dirX, dirY);
            v.Normalize();

            Degree roll = new Degree(new Radian((float)Math.Acos(Utilities.Clamp(v.X, -1.0f, 1.0f))));

            if (Utilities.LessThan(v.Y, 0.0f))
            {
                roll = new Degree(kMaxRoll) - roll;
            }
            
            float ret = (roll.Value / kHalfMaxRoll);

            return ret;
        }

        private static void _CalculateYawHelperHelper(List<float> aData, ref float arTotalCom, ref float arTotalCount)
        {
            int window = (int)aData.Count;
            int windowCenter = window / 2;
            float windowScale = (float)(1.0 / (double)windowCenter);
            
            if ((int)aData.Count >= window)
            {
                for (int i = 0; i + window <= (int)aData.Count; i++)
                {
                    float com  = 0.0f;
                    float mass = 0.0f;
                    
                    for (int j = i; j < i + window; j++)
                    {
                        com  += (float)(j - i - windowCenter) * aData[j];
                        mass += aData[j];
                    }
                    
                    if (Utilities.GreaterThan(mass, 0.0f))
                    {
                        com /= mass;
                        com *= windowScale;
                        
                        arTotalCom += com;
                        arTotalCount += 1.0f;
                    }
                }
            }    
        }
        private static void _CalculateYawHelper(ImageData aData, float xSlope, float ySlope, float fX, float fY, ref float arTotalCom, ref float arTotalCount)
        {
            float skFactor = (float)(1.0 / 255.0);
            byte[] p = aData.Image;
            int width = aData.Width;
            int height = aData.Height;
            
            int x = (int)fX;
            int y = (int)fY;

            List<float> data = new List<float>();
            
            while (fX >= 0.0f && fY >= 0.0f && x < width && y < height)
            {
                int kIndex = (y * width) + x;
                
                if (aData.IsValid(x, y))
                {
                    float v = skFactor * p[kIndex];
                    
                    data.Add(v);
                }
                else if (data.Count >= kMinimumDataSize)
                {
                    _CalculateYawHelperHelper(data, ref arTotalCom, ref arTotalCount);
                    data.Clear();
                }
                
                fX += xSlope;
                fY += ySlope;
                
                x = (int)fX;
                y = (int)fY;
            }
            
            if (data.Count >= kMinimumDataSize)
            {
                _CalculateYawHelperHelper(data, ref arTotalCom, ref arTotalCount);
            }
        }

        private static float kYawAdjustment = (float)(3.0 / 2.0);

        private static float _CalculateYaw(ImageData aData, Degree aAngle)
        {
            byte[] p = aData.Image;
            int width = aData.Width;
            int height = aData.Height;
            int size = width * height;
            int x0 = aData.X0;
            int y0 = aData.Y0;
            int x1 = aData.X1;
            int y1 = aData.Y1;

            float xSlope = aAngle.Cos();
            float ySlope = aAngle.Sin();
            float x = Utilities.LessThan(xSlope, 0.0f) ? (float)x1 : (float)x0;
            float y = Utilities.LessThan(ySlope, 0.0f) ? (float)y1 : (float)y0;
            
            float totalCom = 0.0f;
            float totalCount = 0.0f;

            for (int xStart = x0; xStart <= x1; xStart++)
            {
                _CalculateYawHelper(aData, xSlope, ySlope, (float)xStart, y, ref totalCom, ref totalCount);
            }     
            
            for (int yStart = y0; yStart <= y1; yStart++)
            {
                _CalculateYawHelper(aData, xSlope, ySlope, x, (float)yStart, ref totalCom, ref totalCount);
            }

            float ret = 0.0f;
            if (totalCount > 0.0f) { ret = Utilities.Clamp((totalCom / totalCount) * kYawAdjustment, -1.0f, 1.0f); }

            return ret;
        }

        private static float _CalculateMaximumIntensity(ImageData aData)
        {        
            float factor = (float)(1.0 / 255.0);

            byte[] p = aData.Image;
            int width = aData.Width;
            int height = aData.Height;
            int size = width * height;
            int x0 = aData.X0;
            int y0 = aData.Y0;
            int x1 = aData.X1;
            int y1 = aData.Y1;

            byte max = 0;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (aData.IsValid(x, y))
                    {
                        int kIndex = (y * width) + x;
                        byte kValue = p[kIndex];
                        
                        max = Utilities.Max(max, kValue);
                    }
                }
            }

            float ret = max * factor;

            return ret;
        }
        private static void _CalculateLighting(ImageData aData, out ImageIlluminationMetrics arMetrics)
        {
            arMetrics.MaxIntensity = _CalculateMaximumIntensity(aData);
            arMetrics.Entropy = _CalculateEntropy(aData);
            arMetrics.Roll = _CalculateRoll(aData);
            arMetrics.Yaw = _CalculateYaw(aData, new Degree((arMetrics.Roll / ImageIlluminationMetrics.kRollMax) * kMaxRoll));
        }
        #endregion

#if DEBUG
        static readonly string kSaveFilename = "filtered_capture.bmp";
        static bool msbFirst = true;
#endif

        public static void ExtractLighting(string aFilename, out ImageIlluminationMetrics arOut)
        {
            using (System.Drawing.Image image = System.Drawing.Bitmap.FromFile(aFilename))
            {
                using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image))
                {
                    const int stride = 4;
                    int width = bitmap.Width;
                    int pitch = width * stride;
                    int height = bitmap.Height;
                    int size = (stride * width * height);
                    byte[] data = new byte[size];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int i = (y * pitch) + (x * stride);

                            System.Drawing.Color imgColor = bitmap.GetPixel(x, y);

                            Color color = new Color(imgColor.R, imgColor.G, imgColor.B, imgColor.A);

                            Utilities.SetPixel(SurfaceFormat.Color, data, i, color);
                        }
                    }

                    LightExtractorImage extractorImage;
                    extractorImage.Data = data;
                    extractorImage.Format = SurfaceFormat.Color;
                    extractorImage.Height = height;
                    extractorImage.Width = width;

                    ExtractLighting(ref extractorImage, out arOut);
                }
            }
        }

        public static void ExtractLighting(ref LightExtractorImage aImage, out ImageIlluminationMetrics arOut)
        {
            byte[] dataCopy = new byte[aImage.Data.Length]; aImage.Data.CopyTo(dataCopy, 0);
            ImageData data = new ImageData(aImage.Width, aImage.Height, aImage.Format, dataCopy);

#if DEBUG
            if (msbFirst)
            {
                data.Save(kSaveFilename);
                msbFirst = false;
            }
#endif

            _CalculateLighting(data, out arOut);
        }
    }

}
