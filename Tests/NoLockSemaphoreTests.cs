using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dem0n13.Utils;
using NUnit.Framework;

namespace Dem0n13.Tests
{
    [TestFixture]
    public class NoLockSemaphoreTests
    {
        [Test]
        public void SingleThread()
        {
            var semaphore = new NoLockSemaphore(0, 3);
            Assert.AreEqual(0, semaphore.CurrentCount);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);

            semaphore.Release(2);
            Assert.AreEqual(3, semaphore.CurrentCount);
            Assert.Throws<SemaphoreFullException>(semaphore.Release);

            Assert.IsTrue(semaphore.TryTake());
            Assert.AreEqual(2, semaphore.CurrentCount);

            Assert.IsTrue(semaphore.TryTake());
            Assert.IsTrue(semaphore.TryTake());
            Assert.AreEqual(0, semaphore.CurrentCount);

            Assert.IsFalse(semaphore.TryTake());
            Assert.AreEqual(0, semaphore.CurrentCount);
        }

        [Test]
        public void MultiThread()
        {
            const int threadCount = 25;
            const int iterations = 25;
            const int resourceCount = 4;

            var semaphore = new NoLockSemaphore(resourceCount);
            Assert.AreEqual(resourceCount, semaphore.CurrentCount);

            var factory = new TaskFactory(TaskScheduler.Default);
            ThreadPool.QueueUserWorkItem(state => { });
            var tasks = new Task[threadCount];
            var currentTreadCount = 0;
            var fails = 0;

            for (var t = 0; t < threadCount; t++)
            {
                tasks[t] = factory.StartNew(
                    () =>
                        {
                            for (var i = 0; i < iterations; i++)
                            {
                                if (semaphore.TryTake())
                                {
                                    Interlocked.Increment(ref currentTreadCount);
                                    Assert.LessOrEqual(currentTreadCount, resourceCount);
                                    Thread.Sleep(10);
                                    Interlocked.Decrement(ref currentTreadCount);
                                    semaphore.Release();
                                }
                                else
                                {
                                    Interlocked.Increment(ref fails);
                                }
                            }
                        });
            }
            Task.WaitAll(tasks);
            Debug.WriteLine("Fails: " + fails);
            Assert.AreEqual(resourceCount, semaphore.CurrentCount);
        }
    }
}