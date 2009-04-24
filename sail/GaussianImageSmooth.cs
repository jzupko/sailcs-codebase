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

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using siat;

namespace sail
{

    public struct GaussianCoefficients
    {
        public GaussianCoefficients(int dummy)
        {
            b = new float[4];
            B = 0.0f;
        }

        public float[] b;
        public float B;
    };

    /// <summary>
    /// Smooths an image with a gaussian kernel.
    /// </summary>
    /// <remarks>
    /// Uses the algorithm from: Young, I. T., Van Vliet, L. J., 
    ///     "Recursive implementation of the Gaussian filter", Signal Processing, 44, 139-151.
    /// </remarks>
    public static class GaussianImageSmooth
    {
        public const int kGaussianSmoothPadding = 3;
        public const double kRetinexKernelRadius = 1.0;
        public static readonly float kRetinexStdDev = (float)Math.Sqrt(-((kRetinexKernelRadius + 1.0) * (kRetinexKernelRadius + 1.0)) / (2.0 * Math.Log(1.0 / 255.0)));

        #region Private members
        private static void _PopulateGaussianCoefficients(float aStdDev, ref GaussianCoefficients arC)
        {
            float q = 0.0f;
            
            if (aStdDev > 2.5f || Utilities.AboutEqual(aStdDev, 2.5f))
            {
                q = (0.98711f * aStdDev) - 0.96330f;
            }
            else if (aStdDev > 0.5f || Utilities.AboutEqual(aStdDev, 0.5f))
            {
                q = 3.97156f - (4.14554f * (float)Math.Sqrt(1.0f - (0.26891f * aStdDev)));
            }
            else
            {
                q = 0.1147705f;
            }

            float q2 = q * q;
            float q3 = q * q2;

            arC.b[0] = 1.57825f + (2.44413f * q)  + (1.4281f * q2) + (0.422205f * q3);
            arC.b[1] = (2.44413f * q) + (2.85619f  * q2) + (1.26661f  * q3);
            arC.b[2] = -((1.4281f * q2) + (1.26661f  * q3));
            arC.b[3] = (0.422205f * q3);
            
            arC.B = 1.0f - ((arC.b[1] + arC.b[2] + arC.b[3]) / arC.b[0]);
        }

        private static void _GaussianSmooth(float aStdDev, int aX0, int aY0, int aX1, int aY1, int aWidth, int aHeight, SurfaceFormat aFormat, byte[] arImage)
        {
            GaussianCoefficients c = new GaussianCoefficients(0);
            _PopulateGaussianCoefficients(aStdDev, ref c);

            byte[] p = arImage;
            int width = aWidth;
            int height = aHeight;
            int stride = Utilities.GetStride(aFormat);
            int stride2 = stride * 2;
            int stride3 = stride * 3;
            int stride4 = stride * 4;
            int size = p.Length;
            int padding = kGaussianSmoothPadding;
            int pitch = width * stride;
            int pitch2 = width * stride2;
            int pitch3 = width * stride3;
            int pitch4 = width * stride4;

            float[] a = new float[size];
            float[] b = new float[size];
            for (int i = 0; i < size; i++) { a[i] = p[i]; b[i] = p[i]; }

            // forward pass, rows
            for (int y = aY0; y <= aY1; y++)
            {
                for (int x = (aX0 + padding); x <= aX1; x++)
                {
                    int i = (y * pitch) + (x * stride);
                    int i0 = i;
                    int i1 = i - stride;
                    int i2 = i - stride2;
                    int i3 = i - stride3;

                    for (int j = 0; j < stride; j++)
                    {
                        float v0 = p[i0 + j];
                        float v1 = a[i1 + j];
                        float v2 = a[i2 + j];
                        float v3 = a[i3 + j];

                        a[i0 + j] = ((c.B * v0) + (((c.b[1] * v1) + (c.b[2] * v2) + (c.b[3] * v3)) / c.b[0]));
                    }
                }
            }

            // forward pass, columns
            for (int x = aX0; x <= aX1; x++)
            {
                for (int y = (aY0 + padding); y <= aY1; y++)
                {
                    int i = (y * pitch) + (x * stride);
                    int i0 = i;
                    int i1 = i - pitch;
                    int i2 = i - pitch2;
                    int i3 = i - pitch3;

                    for (int j = 0; j < stride; j++)
                    {
                        float v0 = p[i0 + j];
                        float v1 = a[i1 + j];
                        float v2 = a[i2 + j];
                        float v3 = a[i3 + j];

                        a[i0 + j] = ((c.B * v0) + (((c.b[1] * v1) + (c.b[2] * v2) + (c.b[3] * v3)) / c.b[0]));
                    }
                }
            }

            // backward pass, rows
            for (int y = aY0; y <= aY1; y++)
            {
                for (int x = (aX1 - padding); x >= aX0; x--)
                {
                    int i = (y * pitch) + (x * stride);
                    int i0 = i;
                    int i1 = i + stride;
                    int i2 = i + stride2;
                    int i3 = i + stride3;

                    for (int j = 0; j < stride; j++)
                    {
                        float v0 = a[i0 + j];
                        float v1 = b[i1 + j];
                        float v2 = b[i2 + j];
                        float v3 = b[i3 + j];

                        b[i0 + j] = ((c.B * v0) + (((c.b[1] * v1) + (c.b[2] * v2) + (c.b[3] * v3)) / c.b[0]));
                    }
                }
            }
            
            // backward pass, columns
            for (int x = aX0; x <= aX1; x++)
            {
                for (int y = (aY1 - padding); y >= aY0; y--)
                {
                    int i = (y * pitch) + (x * stride);
                    int i0 = i;
                    int i1 = i + pitch;
                    int i2 = i + pitch2;
                    int i3 = i + pitch3;

                    for (int j = 0; j < stride; j++)
                    {
                        float v0 = a[i0 + j];
                        float v1 = b[i1 + j];
                        float v2 = b[i2 + j];
                        float v3 = b[i3 + j];

                        b[i0 + j] = ((c.B * v0) + (((c.b[1] * v1) + (c.b[2] * v2) + (c.b[3] * v3)) / c.b[0]));
                    }
                }
            }

            for (int i = 0; i < size; i++) { p[i] = (byte)b[i]; }
        }
        #endregion

        public static void Calculate(int aX0, int aY0, int aX1, int aY1, int aWidth, int aHeight, SurfaceFormat aFormat, byte[] arImage)
        {
            // Temp:
            float[] test = new float[5];
            for (int i = -2; i <= 2; i++)
            {
                test[i+2] = Utilities.Gaussian1D((float)i, kRetinexStdDev);
            }
            // End temp:

            _GaussianSmooth(kRetinexStdDev, aX0, aY0, aX1, aY1, aWidth, aHeight, aFormat, arImage);
        }
    }

}
