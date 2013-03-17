using System.Collections.Concurrent;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides the thread safely generating unique <see cref="long"/> numbers within specified type.
    /// Generation starts from zero.
    /// Support the returning of unused numbers for reusing.
    /// </summary>
    public class IdGenerator<T>
    {
        #region Singleton implementation

        /// <summary>
        /// Current instance of <see cref="IdGenerator{T}"/>
        /// </summary>
        public static readonly IdGenerator<T> Current = new IdGenerator<T>();

        private IdGenerator()
        {
        }

        #endregion

        private readonly ConcurrentQueue<long> _usedIds = new ConcurrentQueue<long>();
        private long _currentMaxId = -1;

        /// <summary>
        /// Generates a new unique <see cref="long"/> number
        /// </summary>
        /// <returns></returns>
        public long GetNext()
        {
            long id;
            return _usedIds.TryDequeue(out id) ? id : Interlocked.Increment(ref _currentMaxId);
        }

        /// <summary>
        /// Returns unused number for future reusing
        /// </summary>
        /// <param name="id">Number to return</param>
        public void Release(long id)
        {
            _usedIds.Enqueue(id);
        }
    }
}
