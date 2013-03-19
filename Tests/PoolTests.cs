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
        public void Create()
        {
            var pool = new DerivedPool(5);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);

            pool = new DerivedPool(100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void OneThreadTakeReturn()
        {
            const int iterations = 100;

            var pool = new DerivedPool(5);
            var item = pool.Take();
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(4, pool.CurrentCount);

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
        public void ManyThreadsTakeReturnDerived()
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
                        })
                    .Start();
            }

            Thread.Sleep(100);
            pool.WaitAll();
            Assert.GreaterOrEqual(threadCount, pool.TotalCount);
            Debug.WriteLine(pool.TotalCount);
        }

        [Test]
        public void ManyThreadsTakeReturnInjected()
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

        private class DerivedPool : Pool<Derived>
        {
            public DerivedPool(int initialCount)
            {
                Allocate(initialCount);
            }

            protected override Derived CreateNew()
            {
                return new Derived();
            }
        }

        private class InjectedPool : Pool<Injected>
        {
            public InjectedPool(int initialCount)
            {
                Allocate(initialCount);
            }

            protected override Injected CreateNew()
            {
                return new Injected();
            }
        }

        private class Derived : PoolObject<Derived> // for user class
        {
            public int Tag;
        }

        private class ThirdParty
        {
            public int Tag;
        }

        private class Injected : ThirdParty, IPoolable<Injected> // for third party class
        {
            private readonly int _id = IdGenerator<Injected>.Current.GetNext();
            public int Id { get { return _id; } }
        }
    }
}