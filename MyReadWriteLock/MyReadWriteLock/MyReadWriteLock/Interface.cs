using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReadWriteLock
{
    //用来保护读写锁的临界区
    interface IMyLock
    {
        void EnterMyLock();

        void ExitMyLock();
    }

    //读写锁的接口
    interface IMyReadWriteLock
    {

        void EnterReadLock();

        void ExitReadLock();

        void EnterWriteLock();

        void ExitWriteLock();

    }
}
