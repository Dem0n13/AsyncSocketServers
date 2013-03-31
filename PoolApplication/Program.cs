using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dem0n13.Utils;

namespace PoolApplication
{
    class Program
    {
        static void Main()
        {
            const int iterCount = 100000;
            const int tasksCount = 25;
            /*
            Test(OneThreadWithoutPool, iterCount);
            GC.Collect();
            GC.WaitForFullGCComplete();
            
            Test(OneThreadWithPool, iterCount);
            GC.Collect();
            GC.WaitForFullGCComplete();
            
            Test(ManyThreadsWithoutPool, iterCount, tasksCount);
            GC.Collect();
            GC.WaitForFullGCComplete();
*/
            Test(ManyThreadsWithPool, iterCount, tasksCount);
            
            Console.ReadLine();
        }

        private static void Test(Action<int> action, int arg)
        {
            var sw = Stopwatch.StartNew();
            action(arg);
            Console.WriteLine("{0}: {1}", action.Method.Name, sw.Elapsed);
        }

        private static void Test(Action<int, int> action, int arg1, int arg2)
        {
            var sw = Stopwatch.StartNew();
            action(arg1, arg2);
            Console.WriteLine("{0}: {1}", action.Method.Name, sw.Elapsed);
        }

        private static void OneThreadWithoutPool(int iterCount)
        {
            for (var i = 0; i < iterCount; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[1024], 0, 1024);
            }
        }

        private static void OneThreadWithPool(int iterCount)
        {
            var pool = new ThirdPartyPool(1024, 0, 10);
            for (var i = 0; i < iterCount; i++)
            {
                using (var slot = pool.TakeSlot())
                {
                }
            }
        }
        
        private static void ManyThreadsWithoutPool(int iterCount, int taskCount)
        {
            var tasks = new Task[taskCount];
            for (var t = 0; t < taskCount; t++)
            {
                tasks[t] = Task.Factory.StartNew(
                    () =>
                        {
                            for (var i = 0; i < iterCount; i++)
                            {
                                var args = new SocketAsyncEventArgs();
                                args.SetBuffer(new byte[1024], 0, 1024);
                            }
                        });
            }
            Task.WaitAll(tasks);
        }

        private static void ManyThreadsWithPool(int iterCount, int taskCount)
        {
            var tasks = new Task[taskCount];
            var pool = new ThirdPartyPool(1024, 0, 10);

            for (var t = 0; t < taskCount; t++)
            {
                tasks[t] = Task.Factory.StartNew(
                    () =>
                        {
                            for (var i = 0; i < iterCount; i++)
                            {
                                using (var slot = pool.TakeSlot())
                                {
                                }
                            }
                        });
            }
            Task.WaitAll(tasks);
        }
    }
}
