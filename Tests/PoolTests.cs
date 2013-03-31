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
            pool.TakeSlot();
            pool.TakeSlot();
            pool.TakeSlot();
            Assert.AreEqual(3, pool.TotalCount);

            pool = new ThirdPartyPool(100, 100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void OneThreadScenario()
        {
            const int iterations = 100;
            const int initialCount = 5;

            var pool = new ThirdPartyPool(initialCount, 50);
            var item = pool.TakeSlot();
            pool.Release(item);
            Assert.AreEqual(initialCount, pool.TotalCount);
            Assert.AreEqual(initialCount, pool.CurrentCount);
            Assert.Throws<ArgumentException>(() => pool.Release(new ThirdPartyPool(1, 1).TakeSlot()));
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));

            for (var i = 0; i < iterations; i++)
            {
                using (var slot = pool.TakeSlot())
                {
                    Assert.IsFalse(slot.Object.Flag);
                    slot.Object.Flag = true;
                    Assert.AreEqual(initialCount, pool.TotalCount);
                    Assert.AreEqual(initialCount - 1, pool.CurrentCount);
                }
            }
            Assert.AreEqual(initialCount, pool.TotalCount);
            Assert.AreEqual(initialCount, pool.CurrentCount);
        }

        [Test]
        public void MultiThreadsScenario()
        {
            const int iterations = 50;
            const int threadCount = 50;

            var pool = new ThirdPartyPool(10, 50);
            MultiThreadsScenario(threadCount, iterations, pool);
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
            const int iterations = 100;
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

        private void MultiThreadsScenario(int threadCount, int iterations, Pool<ThirdParty> pool)
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
                                using (var slot = pool.TakeSlot())
                                {
                                    Assert.IsFalse(slot.Object.Flag);
                                    slot.Object.Flag = true;
                                }
                            }
                        }
                    );
            }

            Task.WaitAll(tasks);
        }

        internal sealed class ThirdParty
        {
            public bool Flag { get; set; }
        }

        internal sealed class ThirdPartyPool : Pool<ThirdParty>
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

            protected override void CleanUp(ThirdParty @object)
            {
                @object.Flag = false;
            }
        }
    }
}