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

using System;
using System.Collections.Generic;

namespace siat
{

    public class VarMatrix
    {
        #region Private members
        private int _I(int aRow, int aColumn)
        {
            return (aColumn * mRows) + aRow;
        }

        private int mRows = 0;
        private int mCols = 0;
        private float[] mData = new float[0];
        #endregion

        public VarMatrix() { }
        public VarMatrix(int aRows, int aCols)
        {
            mRows = aRows;
            mCols = aCols;
            mData = new float[mRows * mCols];
        }

        public float this[int r, int c] { get { return mData[_I(r, c)]; } set { mData[_I(r, c)] = value; } }

        public static VarMatrix operator *(VarMatrix a, VarMatrix b) { return a.Multiply(b); }

        public int Cols { get { return mCols; } }
        void MakeZero() { Array.Clear(mData, 0, mData.Length); }
        public int Rows { get { return mRows; } }
        public int Size { get { return mData.Length; } }
        public void Swap(int r0, int c0, int r1, int c1) { Utilities.Swap(ref mData[_I(r0, c0)], ref mData[_I(r1, c1)]); }

        public VarMatrix Clone()
        {
            VarMatrix ret = new VarMatrix(mRows, mCols);
            mData.CopyTo(ret.mData, 0);

            return ret;
        }

        public VarMatrix GetTranspose()
        {
            VarMatrix ret = new VarMatrix(mCols, mRows);

            for (int r = 0; r < mRows; r++)
            {
                for (int c = 0; c < mCols; c++)
                {
                    ret[c, r] = mData[_I(r, c)];
                }
            }

            return ret;
        }

        public VarMatrix Multiply(VarMatrix m)
        {
            if (mCols != m.mRows)
            {
                throw new ArgumentException();
            }

            VarMatrix ret = new VarMatrix(mRows, m.mCols);
            ret.MakeZero();

            for (int c2 = 0; c2 < m.mCols; c2++)
            {
                for (int r = 0; r < mRows; r++)
                {
                    for (int c = 0; c < mCols; c++)
                    {
                        ret[r, c2] += mData[_I(r, c)] * m[c, c2];
                    }
                }
            }

            return ret;
        }

        public void SwapRows(int r1, int r2)
        {
            if (r1 >= mRows || r2 >= mRows)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            for (int c = 0; c < mCols; c++)
            {
                Utilities.Swap(ref mData[_I(r1, c)], ref mData[_I(r2, c)]);
            }
        }

        public bool ToInverse(out VarMatrix arOut)
        {
            if (!(mRows > 0) || (mRows != mCols))
            {
                arOut = new VarMatrix();
                return false;
            }

            int size = mRows;
            arOut = Clone();

            bool[] bPivoted = new bool[size];
            Array.Clear(bPivoted, 0, bPivoted.Length);

            int[] cols = new int[size];
            int[] rows = new int[size];

            for (int i = 0; i < size; i++)
            {
                float max = float.MinValue;
                int r = 0;
                int c = 0;

                for (int j = 0; j < size; j++)
                {
                    if (!bPivoted[j])
                    {
                        for (int k = 0; k < size; k++)
                        {
                            if (!bPivoted[k])
                            {
                                float abs = Math.Abs(arOut[j, k]);

                                if (Utilities.GreaterThan(abs, max))
                                {
                                    max = abs;
                                    r = j;
                                    c = k;
                                }
                            }
                        }
                    }
                }

                if (Utilities.AboutZero(max))
                {
                    return false; // Singular.
                }

                bPivoted[c] = true;

                if (r != c)
                {
                    arOut.SwapRows(r, c);
                }

                rows[i] = r;
                cols[i] = c;

                float inv = 1.0f / arOut[c, c];
                arOut[c, c] = 1.0f;

                for (int j = 0; j < size; j++)
                {
                    arOut[c, j] *= inv;
                }

                for (int j = 0; j < size; j++)
                {
                    if (j != c)
                    {
                        float a = arOut[j, c];
                        arOut[j, c] = 0.0f;

                        for (int k = 0; k < size; k++)
                        {
                            arOut[j, k] -= (arOut[c, k] * a);
                        }
                    }
                }
            }

            for (int i = size - 1; i >= 0; i--)
            {
                if (rows[i] != cols[i])
                {
                    for (int j = 0; j < size; j++)
                    {
                        float a = arOut[j, rows[i]];

                        arOut[j, rows[i]] = arOut[j, cols[i]];
                        arOut[j, cols[i]] = a;
                    }
                }
            }

            return true;
        }
    };

    /// <summary>
    /// Solves Ax = B system of linear equations.
    /// </summary>
    public static class LinearSystemSolver
    {
        /// <summary>
        /// Solves the system, outputting the result to BX.
        /// </summary>
        /// <param name="A">An m x m matrix of coefficients.</param>
        /// <param name="BX">An m x 1 matrix used as input and for writing output.</param>
        /// <returns>True if successful, false if solve failed.</returns>
        public static bool Solve(VarMatrix A, VarMatrix BX)
        {
            if (A.Rows != A.Cols || A.Rows <= 0)
            {
                return false;
            }
            
            int count = A.Cols;
            
            VarMatrix M = A.Clone();
            VarMatrix N = new VarMatrix(count, 1);

            for (int i = 0; i < count; i++)
            {
                float max = 0.0f;

                for (int j = 0; j < count; j++)
                {
                    float value = Math.Abs(M[i, j]);
                    
                    if (Utilities.GreaterThan(value, max))
                    {
                        max = value;
                    }
                }
                
                if (Utilities.AboutZero(max))
                {
                    return false;
                }
                else
                {
                    N[i, 0] = 1.0f / max;
                }
            }

            for (int j = 0; j < count - 1; j++)
            {
                int row = -1;
                float max = 0.0f;

                for (int i = j; i < count; i++)
                {
                    float value = Math.Abs(M[i, j]) * N[i, 0];

                    if (Utilities.GreaterThan(value, max))
                    {
                        max = value;
                        row = i;
                    }
                }

                if (row != j)
                {            
                    if (row == -1)
                    {
                        return false;
                    }

                    for (int k = 0; k < count; k++)
                    {
                        M.Swap(j, k, row, k);
                    }

                    BX.Swap(j, 0, row, 0);
                    N.Swap(j, 0, row, 0);
                }
                
                float denominator = 1.0f / M[j, j];

                for (int i = j + 1; i < count; i++)
                {
                    float factor = M[i, j] * denominator;

                    BX[i, 0] -= BX[j, 0] * factor;

                    for (int k = 0; k < count; k++)
                    {
                        M[i, k] -= M[j, k] * factor;
                    }
                }
            }

            for (int i = count - 1; i >= 0; i--)
            {        
                float sum = BX[i, 0];

                for (int j = i + 1; j < count; j++)
                {
                    sum -= M[i, j] * BX[j, 0];
                }
                
                BX[i, 0] = sum / M[i, i];
            }

            return true;
        }
    }

}
