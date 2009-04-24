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

namespace siat
{
    /// <summary>
    /// Used to pool objects to avoid unnecessary garbage generation.
    /// </summary>
    /// <remarks>
    /// Note that this pool has very specific semantics. It is expected that the objects used by
    /// this pool are allocated for a task and then all dumped at once, to immediately be reallocated
    /// to a similar task. Therefore, objets are never explicitly freed, but the static global function
    /// Reset() is called, which simply makes all objects in the Pool available for allocation again.
    /// 
    /// \warning Using this Pool without regularly calling Reset() will result in the size of the Pool
    ///          increasing without bound.
    /// 
    /// \warning Calling Reset() effectively invalidates all Pooled objects. All references to objects in
    ///          the pool should be invalidated with the call to Reset() however this is accomplished within the
    ///          context of the Pool's usage.
    /// </remarks>
    public static class ResetPool<T>
        where T : class, new()
    {
        #region Private members
        private static T[] msPool = new T[kInitialPoolSize];
        private static int msCount = 0;
        private static int msStorage = kInitialPoolSize;

        static ResetPool()
        {
            for (int i = 0; i < msStorage; i++)
            {
                msPool[i] = new T();
            }

            msCount = msStorage;
        }
        #endregion

        public const int kInitialPoolSize = 4096;
        public const int kGrowthMultiplier = 2;

        public static T Grab()
        {
            if (msCount == 0)
            {
                T[] t = msPool;
                msCount = msStorage;
                msStorage *= kGrowthMultiplier;
                msPool = new T[msStorage];

                for (int i = 0; i < msCount; i++)
                {
                    msPool[i] = new T();
                }

                t.CopyTo(msPool, msCount);
            }

            int index = msCount - 1;
            T obj = msPool[index];
            msCount--;

            return obj;
        }

        public static void Reset()
        {
            msCount = msStorage;
        }
    }
}
