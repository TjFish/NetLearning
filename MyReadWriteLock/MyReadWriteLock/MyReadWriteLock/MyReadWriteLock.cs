using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MyReadWriteLock
{

    /*读写锁理解：
    * 与传统的独占锁不同：读写锁可以共享读，但只能一个写。
    * 在读操作远多于写情况下，读写锁性能相较于独占锁更优。独占锁的效率低主要原因是高并发下临界区的激烈竞争导致线程上下文切换。
    * 但当并发不是很高的情况下，读写锁由于需要额外维护读锁的状态，可能还不如独占锁的效率高。因此需要根据实际情况选择使用。
    * 因此，读写锁的适用场景是，读操作数量远大于写操作数量。
    * 
    * 读写锁实现思路:
    * 1. 使用互锁函数族（Interlocked）实现的自旋锁（SpinLock）对读写锁本身临界区的保护。
    * 
    * 2. 使用事件（AutoRestEvent，ManualResetEvent）让读者/写者等待，避免忙等。事件触发唤醒等待中的写者和读者
    * 
    * 3. 调度策略：
    *   3.1 读者获取锁：读者到来时，判断是否已有写者或等待中的写者，如果没有写者，则直接进入读模式，读计数+1。
    *       如果有，则进入睡眠模式，等待所有写者完成工作，以此避免写饥饿。
    *       
    *   3.2 写者获取锁：写者到来时,关闭读事件，后续读者均无法获取读锁，避免写饥饿。
    *       之后判断是否已有读者或写者正在工作，如果有，则写等待计数+1，写者进入睡眠模式。如果没有，则获取写锁，写者工作。
    *       
    *   3.3 读者释放锁：释放锁时，判断自己是否是最后一个读者，如果还有其他读者正在读，则读计数-1，直接退出。
    *       如果是最后一位读者，就判断是否有等待中写者，有则触发写事件唤醒一位写者（AutoResetEvent）。
    *       
    *   3.4 写者释放锁：写者释放锁时，判断是否有写者等待，如果有，触发写事件唤醒一位写者
    *       如果没有等待的写者，就开启读事件（ManualResetEvent），唤醒等待的读者。
    *       
    *   3.5 注意：事件触发仅仅用于唤醒等待的线程，以此避免忙等。但并不代表被唤醒后，线程一定能获取读写锁，
    *            线程需要继续参与竞争，根据上述判断条件竞争读写锁。
    *            
    * 4. 实现特点：
    *     4.1 不可重入：写者不可重复获取写锁，不然会导致死锁
    *     
    *     4.2 不支持锁升级和锁降级:写者必须释放写锁才能获取读锁，读者也必须释放读锁才能获取写锁，不然会出现死锁。
    *     
    *     4.3 自旋锁实现临界区保护：由于临界区操作代码短执行快，读写锁临界区冲突可能性较小，
    *         因此相较于Mutex实现临界区保护可以减少用户态到内核态的开销，以及线程上下文切换的浪费。
    *         此外，本自旋锁在自旋达到最大次数后，会主动放弃线程执行，避免忙等。
    *         实际测试发现，读写100000次（TestCase2）Mutex实现需要1200ms，忙等的自旋锁实现需要160ms
    *         自旋锁实现只需104ms,官方实现ReaderWriterLockSlim 需要82ms。
    *         
    * 5. 进一步优化：
    *      参考C# ReaderWriterLockSlim 源码实现，可以发现，其读者写者冲突时，并不是立即让线程睡眠，而是先自旋一定次数，
    *      每次自旋中持续竞争读写锁。直到达到最大自旋次数还未获取读写锁，才让线程睡眠等待唤醒事件。
    *      因此可以从这个方面进一步去优化读写锁。
    * 
    */
    class MyReadWriteLock :IMyReadWriteLock
    {
        private int readCount;

        private int writeCount;

        private int writeWaitCount;

        //拿到写锁的线程id，避免重入
        private int writeThreadId;

        private IMyLock myLock;

        private ManualResetEvent readEvent;
        private AutoResetEvent writeEvent;


        public MyReadWriteLock()
        {
            readCount = 0;
            writeCount = 0;
            writeWaitCount = 0;
            writeThreadId = -1;
            //自旋锁实现临界区保护
            myLock = new Spain_MyLock();
            //myLock = new Mutex_MyLock();
            readEvent = new ManualResetEvent(false);
            writeEvent = new AutoResetEvent(false);
        }
        private void EnterMyLock()
        {
            //进入临界区
            myLock.EnterMyLock();
        }
        private void ExitMyLock()
        {
            //退出临界区
            myLock.ExitMyLock();
        }

        public void EnterReadLock()
        {
            EnterMyLock();
            while(true)
            {
                //此时没有写者，也没有写者等待,读者直接读取
                //如果有等待中的写者，新进的读者就不能获取读锁，避免写饥饿
                if (writeCount==0 && writeWaitCount == 0)
                {
                    readCount++;
                    break;
                }
                else 
                {
                    //先释放临界区锁，避免死锁
                    ExitMyLock();
                    //读者等待读事件，避免忙等
                    readEvent.WaitOne();
                    //读事件触发，重新获取临界区锁
                    EnterMyLock();
                }
            }
            ExitMyLock();
        }

        public void ExitReadLock()
        {
            EnterMyLock();
            readCount--;
            //如果是最后一位读者释放读锁，并且有等待中的写者，就触发写事件，通知写者
            if (readCount==0 && writeWaitCount>0)
            {
                writeEvent.Set();
            }
            ExitMyLock();
        }

        public void EnterWriteLock()
        {
            int id = Environment.CurrentManagedThreadId;
            if(id==writeThreadId)
            {
                throw new Exception("锁不可重入");
            }
            EnterMyLock();
            //关闭读事件，在有写者工作/等待时，所有读者都将等待读事件
            readEvent.Reset();
            while (true)
            {
                //此时没有写者，也没有读者,写者直接获取写锁
                if (writeCount == 0 && readCount == 0)
                {
                    writeCount++;
                    writeThreadId = id;
                    break;
                }
                else
                {
   
                    //写者先登记
                    writeWaitCount++;
                    //释放临界区锁，避免死锁
                    ExitMyLock();
                    //写者等待写事件，避免忙等
                    writeEvent.WaitOne();
                    //写事件触发，重新获取临界区锁
                    EnterMyLock();
                    writeWaitCount--;
                }
            }
            ExitMyLock();
        }

        public void ExitWriteLock()
        {
            EnterMyLock();
            writeCount--;
            writeThreadId = -1;

            //Debug.Assert(writeCount == 0);
            //如果有等待中的写者，就触发写事件，通知写者
            if (writeWaitCount > 0)
            {
                writeEvent.Set();
                ExitMyLock();
                return;
            }
            //如果没有等待的写者，就开启读事件，唤醒等待的读者
            //当然，此时也可能没有等待的读者，但读事件开启表明此时没有写者
            readEvent.Set();
            ExitMyLock();
        }

    }
}
