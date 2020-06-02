using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MyReadWriteLock
{


    //使用互斥锁实现临界区
    public class Mutex_MyLock : IMyLock
    {
        private Mutex m;

        public Mutex_MyLock(){
            m = new Mutex();
        }

        public void EnterMyLock()
        {
            m.WaitOne();
        }

        public void ExitMyLock()
        {
            m.ReleaseMutex();
        }
    }

    //自旋锁实现临界区
    public class Spain_MyLock : IMyLock
    {
        private int Locked;
        private int MaxSpinCount;
        public Spain_MyLock()
        {
            Locked = 0;
            MaxSpinCount = 20;
        }
        public void EnterMyLock()
        {
            int spinCount = 0;
            //这个代码的意思是，判断Locked是否为1，如果为1一直自旋。
            //如果Locked为0，则修改Locked为1，获取锁，由互锁函数族保证操作的原子性
            while (Interlocked.CompareExchange(ref Locked, 1, 0) == 1)
            {
                //自旋
                spinCount++;
                //自旋达到最大自旋次数后，放弃线程执行，触发线程调度
                if (spinCount>MaxSpinCount)
                {
                    Thread.Sleep(0);
                    spinCount = 0;
                }
            }
        }

        public void ExitMyLock()
        {
            Debug.Assert(Locked == 1,"尝试退出一个并不拥有的锁");
            Interlocked.Add(ref Locked, -1);
        }
    }
}
