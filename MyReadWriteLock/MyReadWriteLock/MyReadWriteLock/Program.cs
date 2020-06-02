using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyReadWriteLock
{
    class Program
    {
        static void Main(string[] args)
        {
            new TestCase1().Test();
            new TestCase2().Test();
            Console.ReadKey();

        }
    }
}
