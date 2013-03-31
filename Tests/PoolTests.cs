using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dem0n13.Utils;
using NUnit.Framework;

namespace Dem0n13.Tests
{
    [TestFixture]
    public class PoolTests
    {
        [Test]
        public void Creation()
        {
            var pool = new ThirdPartyPool(0, 3);
            pool.Take();
            pool.Take();
            pool.Take();
            Assert.AreEqual(3, pool.TotalCount);

            pool = new ThirdPartyPool(100, 100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void OneThreadScenario()
        {
            const int iterations = 100;

            var pool = new ThirdPartyPool(5, 50);
            var item = pool.Take();
            pool.Release(item);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
            Assert.Throws<ArgumentException>(() => pool.Release(new ThirdPartyPool(1, 1).Take()));

            for (var i = 0; i < iterations; i++)
            {
                var slot = pool.Take();
                pool.Release(slot);
            }
        }

        [Test]
        public void MultiThreadsScenarioDerived()
        {
            const int iterations = 50;
            const int threadCount = 50;

            var pool = new ThirdPartyPool(10, 50);

            for (var t = 0; t < threadCount; t++)
            {
                new Thread(
                    () =>
                        {
                            for (var i = 0; i < iterations; i++)
                            {
                                var item = pool.Take();
                                Thread.Sleep(1);
                                Assert.DoesNotThrow(() => pool.Release(item));
                            }
                        })
                    .Start();
            }

            Thread.Sleep(100);
            pool.WaitAll();
            Assert.GreaterOrEqual(threadCount, pool.TotalCount);
            Debug.WriteLine(pool.TotalCount);
        }

        [Test]
        public void MaxCapacity()
        {
            const int capacity0 = 1;
            const int capacity1 = 25;
            const int iterations = 10;
            const int taskCount = 25;

            var pool0 = new ThirdPartyPool(capacity0, capacity0);
            var sw = Stopwatch.StartNew();
            MultiThreadsScenario(taskCount, iterations, pool0);
            pool0.WaitAll();
            Debug.WriteLine(sw.Elapsed);
            Assert.AreEqual(capacity0, pool0.TotalCount);

            var pool1 = new ThirdPartyPool(capacity1, capacity1);
            sw.Restart();
            MultiThreadsScenario(taskCount, iterations, pool1);
            pool1.WaitAll();
            Debug.WriteLine(sw.Elapsed);
            Assert.AreEqual(capacity1, pool1.TotalCount);
        }

        private void MultiThreadsScenario(int threadCount, int iterations, ThirdPartyPool pool)
        {
            var factory = new TaskFactory(TaskScheduler.Default);
            ThreadPool.QueueUserWorkItem(state => { });
            var tasks = new Task[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                tasks[t] = factory.StartNew(
                    () =>
                        {
                            for (var i = 0; i < iterations; i++)
                            {
                                var item = pool.Take();
                                Thread.Sleep(10);
                                pool.Release(item);
                            }
                        }
                    );
            }

            Task.WaitAll(tasks);
        }

        internal class ThirdParty
        {
            public int Tag;
        }

        internal class ThirdPartyPool : Pool<ThirdParty>
        {
            public ThirdPartyPool(int initialCount, int maxCapacity)
                : base(maxCapacity)
            {
                TryAllocatePush(initialCount);
            }

            protected override ThirdParty ObjectConstructor()
            {
                return new ThirdParty();
            }
        }
    }
}