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

using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace siat
{
    public static class Cache
    {
        public const long kDefaultMaximumCacheSize = (1 << 29);

        #region Private members
        private static long msTotalCacheSize = 0u;
        private static long msMaximumCacheSize = kDefaultMaximumCacheSize;
        private struct CacheEntry
        {
            public long EstimatedDataSize;
            public WeakReference Reference;
        }

        private static Dictionary<string, CacheEntry> msCacheables = new Dictionary<string, CacheEntry>();
        internal static LinkedList<ICacheable> msLRU = new LinkedList<ICacheable>();

        private static void _Purge()
        {
            if (msLRU.Last != null)
            {
                msLRU.Last.Value.Unload();
                msLRU.RemoveLast();
            }
        }

        private static void _AddToTotalSize(long aSize)
        {
            while ((msTotalCacheSize + aSize) > msMaximumCacheSize)
            {
                _Purge();
            }

            msTotalCacheSize += aSize;
        }

        private static WeakReference _New<T>(string aId)
            where T : ICacheable, new()
        {
            CacheEntry entry = new CacheEntry();

            if (File.Exists(aId))
            {
                FileInfo info = new FileInfo(aId);
                entry.EstimatedDataSize = info.Length;
                _AddToTotalSize(entry.EstimatedDataSize);
            }
            else
            {
                entry.EstimatedDataSize = 0;
            }
            
            T obj = new T();
            obj.Filename = aId;

            entry.Reference = new WeakReference(obj);

            msCacheables[aId] = entry;

            return entry.Reference;
        }
        #endregion

        public static T Get<T>(string aId)
            where T : ICacheable, new()
        {
            WeakReference rf = null;

            if (msCacheables.ContainsKey(aId))
            {
                rf = msCacheables[aId].Reference;

                if (rf.IsAlive) { goto done; }
            }

            rf = _New<T>(aId);

            done:
#if !DEBUG
            if (rf.Target is T) { return (T)(rf.Target); }
            else { return default(T); }
#else
            return (T)(rf.Target);
#endif
        }

        public static long MaximumCacheSize
        {
            get { return msMaximumCacheSize; }
            set
            {
                msMaximumCacheSize = value;
                while (msTotalCacheSize > msMaximumCacheSize)
                {
                    _Purge();
                }
            }
        }
    }

    public interface ICacheable
    {
        string Filename { get; set; }
        void Unload();
    }

    public abstract class Cacheable<T> : ICacheable
        where T : class
    {
        #region Private members
        private string mFilename = string.Empty;
        private bool mbLoading = false;
        private ContentManager mManager = new ContentManager(Siat.Singleton.Services);
        private LinkedListNode<ICacheable> mNode = null;
        private T mObject;

        private void __HandleLoad(object aObject)
        {
            lock (this)
            {
                if (mObject == null)
                {
                    mObject = mManager.Load<T>(mFilename);
                    _OnLoadInThread();
                }
            }
        }
        private WaitCallback _HandleLoad;

        private T _LoadImmediate()
        {
            lock (this)
            {
                if (mObject == null)
                {
                    if (!mbLoading)
                    {
                        mbLoading = true;
                        {
                            mObject = mManager.Load<T>(mFilename);
                            _OnLoadInThread();
                            _OnLoadInMainThread();
                        }
                        mbLoading = false;
                    }
                }

                return mObject;
            }
        }
        #endregion

        #region Protected members
        protected void _Load()
        {
            lock (this)
            {
                if (mObject == null)
                {
                    if (!mbLoading) { mbLoading = ThreadPool.QueueUserWorkItem(_HandleLoad); }
                }
                else if (mbLoading)
                {
                    _OnLoadInMainThread();
                    mbLoading = false;
                }
            }
        }

        protected T _Object
        {
            get
            {
                lock (this)
                {
                    return mObject;
                }
            }
        }

        protected void _Tickle()
        {
            if (mNode != null)
            {
                Cache.msLRU.Remove(mNode);
                mNode = null;
            }

            mNode = Cache.msLRU.AddFirst(this);
        }

        protected T _WaitForObject
        {
            get
            {
                return _LoadImmediate();
            }
        }

        protected abstract void _OnLoadInThread();
        protected abstract void _OnLoadInMainThread();
        protected abstract void _OnUnload();
        #endregion

        #region Internal members
        internal Cacheable()
        {
            _HandleLoad = __HandleLoad;
        }
        #endregion

        public string Filename { get { return mFilename; } set { mFilename = value; } }

        public void Unload()
        {
            lock (this)
            {
                _OnUnload();
                mObject = null;
                mManager.Unload();
            }
        }
    }
}
