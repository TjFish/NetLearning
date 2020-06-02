using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyReadWriteLock
{
    //使用读写锁创建一个多线程共享的缓冲区（字典）,用来验证读写锁的正确性，代码参考MSDN
    public class SynchronizedCache
    {
        private MyReadWriteLock cacheLock = new MyReadWriteLock();
        private Dictionary<int, string> innerCache = new Dictionary<int, string>();

        public int Count
        { get { return innerCache.Count; } }

        public string Read(int key)
        {
            cacheLock.EnterReadLock();
            try
            {
                return innerCache[key];
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void Add(int key, string value)
        {
            cacheLock.EnterWriteLock();
            try
            {
                innerCache.Add(key, value);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }


        public void Delete(int key)
        {
            cacheLock.EnterWriteLock();
            try
            {
                innerCache.Remove(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public enum AddOrUpdateStatus
        {
            Added,
            Updated,
            Unchanged
        };

        ~SynchronizedCache()
        {
            //if (cacheLock != null) cacheLock.Dispose();
        }
    }
}
