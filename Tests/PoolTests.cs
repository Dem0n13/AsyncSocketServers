using System;
using System.Diagnostics;
using System.Threading;
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
            var pool = new DerivedPool(0);
            pool.Take();
            pool.Take();
            pool.Take();
            Assert.AreEqual(3, pool.TotalCount);

            pool = new DerivedPool(100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void Resurect()
        {
            var pool = new DerivedPool(0);
            pool.Take();
            pool.Take();
            pool.Take();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.AreEqual(3, pool.CurrentCount);
        }

        [Test]
        public void OneThreadScenario()
        {
            const int iterations = 100;

            var pool = new DerivedPool(5);
            var item = pool.Take();
            pool.Release(item);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));

            var anotherPool = new DerivedPool(0);
            Assert.Throws<ArgumentException>(() => pool.Release(anotherPool.Take()));

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

            var pool = new DerivedPool(10);

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

            var pool = new InjectedPool(10);

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

        private class Derived : PoolObject<Derived> // for user classes
        {
            public int Tag;

            public Derived(Pool<Derived> pool) 
                : base(pool)
            {
            }
        }

        private class ThirdParty
        {
            public int Tag;
        }

        private class DerivedPool : Pool<Derived>
        {
            public DerivedPool(int initialCount)
            {
                AllocatePush(initialCount);
            }

            protected override Derived ObjectConstructor()
            {
                return new Derived(this);
            }
        }

        private class Injected : ThirdParty, IPoolable<Injected> // for third party classes
        {
            public PoolToken<Injected> PoolToken { get; private set; }

            public Injected(Pool<Injected> pool)
            {
                PoolToken = new PoolToken<Injected>(this, pool);
            }
        }

        private class InjectedPool : Pool<Injected>
        {
            public InjectedPool(int initialCount)
            {
                AllocatePush(initialCount);
            }

            protected override Injected ObjectConstructor()
            {
                return new Injected(this);
            }
        }
    }
}