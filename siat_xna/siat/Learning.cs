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

using System.Collections.Generic;

namespace siat
{
    public static class Stat
    {
        public static void SimpleCubicRegression(List<float> aResponses, List<float> aPredictions, out float arIntercept, out float arLinearSlope, out float arQuadraticSlope, out float arCubicSlope)
        {
            float sumX2 = 0.0f;
            float sumX3 = 0.0f;
            float sumX4 = 0.0f;
            float sumX5 = 0.0f;
            float sumX6 = 0.0f;
            float sumX3Y = 0.0f;
            float sumX2Y = 0.0f;

            uint n = 0;

            for (int i = 0; i < aPredictions.Count; i++)
            {
                float R = aResponses[i];
                float P = aPredictions[i];

                float x2 = (P * P);
                float x3 = (P * x2);
                float x4 = (x2 * x2);
                float x5 = (x2 * x3);
                float x6 = (x3 * x3);
                float x3y = (x3 * R);
                float x2y = (x2 * R);

                sumX2 += x2;
                sumX3 += x3;
                sumX4 += x4;
                sumX5 += x5;
                sumX6 += x6;
                sumX3Y += x3y;
                sumX2Y += x2y;

                n++;
            }

            if (n > 1)
            {
                VarMatrix A = new VarMatrix(4, 4);
                VarMatrix X = new VarMatrix(4, 1);
                VarMatrix Ainv = new VarMatrix(4, 4);

                float x0 = aPredictions[0];
                float x02 = x0 * x0;
                float x03 = x02 * x0;
                float xn = aPredictions[aPredictions.Count-1];
                float xn2 = xn * xn;
                float xn3 = xn2 * xn;
                float y0 = aResponses[0];
                float yn = aResponses[aResponses.Count-1];

                A[0,0] = sumX6; A[0,1] = sumX5; A[0,2] = sumX4; A[0,3] = sumX3;
                A[1,0] = sumX5; A[1,1] = sumX4; A[1,2] = sumX3; A[1,3] = sumX2;
                A[2,0] = x03;   A[2,1] = x02;   A[2,2] = x0;    A[2,3] = 1.0f;
                A[3,0] = xn3;   A[3,1] = xn2;   A[3,2] = xn;    A[3,3] = 1.0f;
                
                X[0,0] = sumX3Y;
                X[1,0] = sumX2Y;
                X[2,0] = y0;
                X[3,0] = yn;

                if (A.ToInverse(out Ainv))
                {
                    VarMatrix B = Ainv * X;

                    arIntercept = B[3,0];
                    arLinearSlope = B[2,0];
                    arQuadraticSlope = B[1,0];
                    arCubicSlope = B[0,0];

                    return;
                }
            }

            arIntercept = 0;
            arLinearSlope = 0;
            arQuadraticSlope = 0;
            arCubicSlope = 0;
        }

        public static void SimpleLinearRegression(List<float> aResponses, List<float> aPredictions, out float arIntercept, out float arSlope)
        {
            float sumX = 0.0f;
            float sumY = 0.0f;
            float sumXY = 0.0f;
            float sumX2 = 0.0f;

            int n = 0;
            
            for (int i = 0; i < aPredictions.Count; i++)
            {
                float R = aResponses[i];
                float P = aPredictions[i];

                sumX  += P;
                sumY  += R;
                sumXY += (P * R);
                sumX2 += (P * P);
                n++;
            }
            
            arSlope = (((float)n * sumXY) - (sumX * sumY)) / (((float)n * sumX2) - (sumX * sumX));
            arIntercept = (sumY - (arSlope * sumX)) / (float)n;
        }

        public static bool MultipleLinearRegression(VarMatrix Y, VarMatrix X, VarMatrix B)
        {
            VarMatrix Xt = X.GetTranspose();
            VarMatrix XtX = Xt * X;
            VarMatrix XTinv = null;
            
            if (!XtX.ToInverse(out XTinv))
            {
                return false;
            }
            
            B = (XTinv * Xt * Y);
            
            return true;
        }

        public static bool Regression(List<float> aResponses, List<float> aPredictions, List<float> arCoefficients)
        {
            int predictionCount = (int)(aPredictions.Count / aResponses.Count);        
            
            if (predictionCount == 1)
            {
                float intercept = 0;
                float slope     = 0;
                
                SimpleLinearRegression(aResponses, aPredictions, out intercept, out slope);
                
                arCoefficients[0] = intercept;
                arCoefficients[1] = slope;
            }
            else
            {
                VarMatrix Y = new VarMatrix((int)aResponses.Count, 1);
                VarMatrix X = new VarMatrix((int)aResponses.Count, predictionCount + 1);
                VarMatrix B = new VarMatrix(predictionCount + 1, 1);
                
                for (int i = 0; i < Y.Rows; i++)
                {
                    Y[i,0] = aResponses[i];
                    X[i,0] = 1.0f;
                    
                    for (int j = 1; j < predictionCount + 1; j++)
                    {
                        int kIndex = (i * predictionCount) + (j-1);
                        
                        X[i,j] = aPredictions[kIndex];
                    }
                }
                
                if (!MultipleLinearRegression(Y, X, B))
                {
                    return false;
                }
                
                for (int i = 0; i < B.Rows; i++)
                {
                    arCoefficients[i] = B[i,0];
                }
            }
            
            return true;
        }
        public static float CoefficientOfDetermination(List<float> aResponses, List<float> aPredictions, List<float> aCoefficients)
        {
            float totalSS = Utilities.Variance(aResponses, Utilities.Mean(aResponses));
            
            List<float> regressions = new List<float>(aResponses.Count);

            for (int i = 0; i < (int)aResponses.Count; i++)
            {
                float entry = aCoefficients[0];
                
                for (int j = 1; j < (int)aCoefficients.Count; j++)
                {
                    int index = (i * (int)(aCoefficients.Count - 1)) + (j-1);

                    entry += aCoefficients[j] * aPredictions[index];
                }
                
                regressions[i] = entry;
            }

            float regreSS = Utilities.Variance(regressions, Utilities.Mean(regressions));
            
            return (regreSS / totalSS);
        }
    }

}
