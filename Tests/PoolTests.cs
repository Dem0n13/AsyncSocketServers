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
            var pool = new DerivedPool(0, 3);
            pool.Take();
            pool.Take();
            pool.Take();
            Assert.AreEqual(3, pool.TotalCount);

            pool = new DerivedPool(100, 100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void Resurect()
        {
            var pool = new DerivedPool(0, 3);
            pool.Take();
            pool.Take();
            pool.Take();
            pool.WaitAll();
            Assert.AreEqual(3, pool.CurrentCount);
        }

        [Test]
        public void OneThreadScenario()
        {
            const int iterations = 100;

            var pool = new DerivedPool(5, 50);
            var item = pool.Take();
            pool.Release(item);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
            Assert.Throws<ArgumentException>(() => pool.Release(new Derived()));

            for (var i = 0; i < iterations; i++)
            {
                var items = new Derived[10];
                for (var j = 0; j < items.Length; j++)
                    items[j] = pool.Take();
                Assert.AreEqual(items.Length, pool.TotalCount);
                Assert.AreEqual(0, pool.CurrentCount);

                foreach (var t in items)
                    pool.Release(t);
                Assert.AreEqual(items.Length, pool.TotalCount);
                Assert.AreEqual(items.Length, pool.CurrentCount);
            }
        }

        [Test]
        public void MultiThreadsScenarioDerived()
        {
            const int iterations = 50;
            const int threadCount = 50;

            var pool = new DerivedPool(10, 50);

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
                            pool.Take();
                        })
                    .Start();
            }

            Thread.Sleep(100);
            pool.WaitAll();
            Assert.GreaterOrEqual(threadCount, pool.TotalCount);
            Debug.WriteLine(pool.TotalCount);
        }

        [Test]
        public void MultiThreadsScenarioInjected()
        {
            const int iterations = 50;
            const int threadCount = 50;

            var pool = new ImplObjectPool(10, 50);

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

            var pool0 = new DerivedPool(capacity0, capacity0, PoolReleasingMethod.Manual);
            var sw = Stopwatch.StartNew();
            MultiThreadsScenario(taskCount, iterations, pool0);
            pool0.WaitAll();
            Debug.WriteLine(sw.Elapsed);
            Assert.AreEqual(capacity0, pool0.TotalCount);

            var pool1 = new DerivedPool(capacity1, capacity1, PoolReleasingMethod.Manual);
            sw.Restart();
            MultiThreadsScenario(taskCount, iterations, pool1);
            pool1.WaitAll();
            Debug.WriteLine(sw.Elapsed);
            Assert.AreEqual(capacity1, pool1.TotalCount);
        }

        private void MultiThreadsScenario<T>(int threadCount, int iterations, Pool<T> pool) where T : IPoolable<T>
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

        internal class Derived : PoolObject<Derived> // for user classes
        {
            public int Tag;

            public Derived()
                : this(null)
            {
            }

            public Derived(Pool<Derived> pool) 
                : base(pool)
            {
            }
        }

        internal class DerivedPool : Pool<Derived>
        {
            public DerivedPool(int initialCount, int maxCapacity, PoolReleasingMethod releasingMethod = PoolReleasingMethod.Auto)
                : base(maxCapacity, releasingMethod)
            {
                TryAllocatePush(initialCount);
            }

            protected override Derived ObjectConstructor()
            {
                return new Derived(this);
            }
        }

        internal class ImplBase
        {
            public int Tag;
        }

        internal class ImplObject : ImplBase, IPoolable<ImplObject>
        {
            public PoolToken<ImplObject> PoolToken { get; private set; }

            public ImplObject()
                : this(null)
            {
            }

            public ImplObject(Pool<ImplObject> pool)
            {
                PoolToken = new PoolToken<ImplObject>(this, pool);
            }
        }

        internal class ImplObjectPool : Pool<ImplObject>
        {
            public ImplObjectPool(int initialCount, int maxCapacity)
                : base(maxCapacity)
            {
                TryAllocatePush(initialCount);
            }

            protected override ImplObject ObjectConstructor()
            {
                return new ImplObject(this);
            }
        }
    }
}