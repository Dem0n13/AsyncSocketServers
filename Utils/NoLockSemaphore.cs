using System;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// A lock-free alternative to the <see cref="Semaphore"/> that controls concurrent access to a resource or resource pool.
    /// </summary>
    public class NoLockSemaphore
    {
        private readonly int _maxCount;
        private int _currentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoLockSemaphore"/> class, specifying the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests that can be granted concurrently.</param>
        public NoLockSemaphore(int initialCount)
            : this(initialCount, int.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoLockSemaphore"/> class, specifying the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests that can be granted concurrently.</param>
        /// <param name="maxCount">The maximum number of requests that can be granted concurrently.</param>
        public NoLockSemaphore(int initialCount, int maxCount)
        {
            if (initialCount < 0 || maxCount < initialCount)
                throw new ArgumentOutOfRangeException("initialCount");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount");

            _currentCount = initialCount;
            _maxCount = maxCount;
        }

        /// <summary>
        /// Gets the maximum number of threads that can be granted concurrently.
        /// </summary>
        public int MaxCount
        {
            get { return _maxCount; }
        }

        /// <summary>
        /// Gets the current number of threads that can be granted concurrently.
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCount; }
        }

        /// <summary>
        /// Attempts to enter the <see cref="NoLockSemaphore"/>
        /// </summary>
        /// <returns>true if the thread is entered successfully, otherwise, false</returns>
        public bool TryTake()
        {
            int oldValue, newValue;
            do
            {
                oldValue = _currentCount;
                newValue = oldValue - 1;
                if (newValue < 0) return false;
            } while (Interlocked.CompareExchange(ref _currentCount, newValue, oldValue) != oldValue);
            return true;
        }

        /// <summary>
        /// Exits the <see cref="NoLockSemaphore"/> once.
        /// </summary>
        public void Release()
        {
            int oldValue, newValue;
            do
            {
                oldValue = _currentCount;
                newValue = oldValue + 1;
                if (newValue > _maxCount) 
                    throw new SemaphoreFullException();
            } while (Interlocked.CompareExchange(ref _currentCount, newValue, oldValue) != oldValue);
        }

        /// <summary>
        /// Exits the <see cref="NoLockSemaphore"/> a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        public void Release(int releaseCount)
        {
            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException("releaseCount", "Release releaseCount is less than 1");

            int oldValue, newValue;
            do
            {
                oldValue = _currentCount;
                newValue = oldValue + releaseCount;
                if (newValue > _maxCount)
                    throw new SemaphoreFullException();
            } while (Interlocked.CompareExchange(ref _currentCount, newValue, oldValue) != oldValue);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}", typeof (NoLockSemaphore).Name, _currentCount, _maxCount);
        }
    }
}