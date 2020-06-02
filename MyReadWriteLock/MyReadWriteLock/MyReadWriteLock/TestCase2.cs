using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyReadWriteLock
{
    //这个TestCase目的是用来测量读写锁的性能
    class TestCase2
    {
        int Count;
        long readWaitTime;
        long writeWaitTime;
        long readExitTime;
        long writeExitTime;
        int MaxCount;
        private IMyReadWriteLock readerWriterLock;
        // 用于监测线程是否执行完毕
        private List<ManualResetEvent> manualResetEvents;

        public TestCase2()
        {
            Count = 0;
            MaxCount = 10000000;
            readWaitTime = 0;
            writeWaitTime = 0;
            readExitTime = 0;
            writeExitTime = 0;
            readerWriterLock = new MyReadWriteLock();
            manualResetEvents = new List<ManualResetEvent>();
        }

        public void Reader(object obj)
        {
            Stopwatch stopwatch = new Stopwatch();
            while(true)
            {
                stopwatch.Start();
                readerWriterLock.EnterReadLock();
                //获取锁后立即记录等待时间
                stopwatch.Stop();
                readWaitTime += stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
                if (Count >= MaxCount)
                {
                    readerWriterLock.ExitReadLock();
                    break;
                }
                stopwatch.Start();
                readerWriterLock.ExitReadLock();
                stopwatch.Stop();
                //退出锁后立即记录退出时间
                Interlocked.Add(ref readExitTime, stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
            }
            ManualResetEvent mre = (ManualResetEvent)obj;
            mre.Set();
        }

        public void Writer(object obj)
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Start();
                readerWriterLock.EnterReadLock();
                stopwatch.Stop();
                writeWaitTime += stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
                if (Count >= MaxCount)
                {
                    readerWriterLock.ExitReadLock();
                    break;
                }
                Count++;
                stopwatch.Start();
                readerWriterLock.ExitReadLock();
                stopwatch.Stop();
                Interlocked.Add(ref writeExitTime, stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
            }
            ManualResetEvent mre = (ManualResetEvent)obj;
            mre.Set();
        }

        public void Test()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int writerThreadNum = 4;
            int readerThreadNum = 40;
            int totalThreadNum = 44;
            for (int i = 0; i < totalThreadNum; i++)
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                manualResetEvents.Add(mre);
                if (i < writerThreadNum)
                {
                    ThreadPool.QueueUserWorkItem(Writer,mre);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(Reader, mre);
                }
            }
            WaitHandle.WaitAll(manualResetEvents.ToArray());
            stopwatch.Stop();
            Console.WriteLine("计数结果{0}，所需时间{1}ms",Count,stopwatch.ElapsedMilliseconds);
            Console.WriteLine("读者等待时间：{0}ms，写者等待时间{1}ms", readWaitTime, writeWaitTime);
            Console.WriteLine("读者平均等待时间：{0}ms，写者平均等待时间{1}ms", readWaitTime/readerThreadNum, writeWaitTime/writerThreadNum);
            Console.WriteLine("读者退出时间：{0}ms，写者退出时间{1}ms", readExitTime, writeExitTime);
            Console.WriteLine("读者平均退出时间：{0}ms，写者平均退出时间{1}ms", readExitTime / readerThreadNum, writeExitTime / writerThreadNum);
        }
    }
}
