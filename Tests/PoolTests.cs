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
            var pool = new MockPool(5);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);

            pool = new MockPool(100);
            Assert.AreEqual(100, pool.TotalCount);
            Assert.AreEqual(100, pool.CurrentCount);
        }

        [Test]
        public void OneThreadTakeReturn()
        {
            const int iterations = 100;

            var pool = new MockPool(5);
            var item = pool.Take();
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(4, pool.CurrentCount);

            pool.Release(item);
            Assert.AreEqual(5, pool.TotalCount);
            Assert.AreEqual(5, pool.CurrentCount);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
            Assert.Throws<ArgumentException>(() => pool.Release(new Mock()));

            for (var i = 0; i < iterations; i++)
            {
                var items = new Mock[10];
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
        public void ManyThreadsTakeReturn()
        {
            const int iterations = 50;
            const int threadCount = 50;

            var pool = new MockPool(10);

            for (var t = 0; t < threadCount; t++)
            {
                new Thread(
                    () =>
                        {
                            for (var i = 0; i < iterations; i++)
                            {
                                var item = pool.Take();

                                Thread.Yield();
                                Assert.DoesNotThrow(() => pool.Release(item));
                            }
                        })
                    .Start();
            }

            Thread.Yield();
            pool.WaitAll();
            Assert.GreaterOrEqual(threadCount, pool.TotalCount);
            Debug.WriteLine(pool.TotalCount);
        }


        private class Mock : UniqueObject<Mock>
        {
        }

        private class MockPool : Pool<Mock>
        {
            public MockPool(int initialCount)
            {
                Allocate(initialCount);
            }

            protected override Mock CreateNew()
            {
                return new Mock();
            }
        }
    }
}