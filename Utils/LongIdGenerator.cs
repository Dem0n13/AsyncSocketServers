using System.Collections.Concurrent;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides the thread safely generating unique <see cref="long"/> numbers within specified type.
    /// Generation starts from zero.
    /// Automaticly reuses old numbers - only for <see cref="UniqueObject{T}"/>.
    /// </summary>
    public class LongIdGenerator<T>
    {
        #region Singleton implementation

        /// <summary>
        /// Current instance of <see cref="LongIdGenerator{T}"/>
        /// </summary>
        public static readonly LongIdGenerator<T> Current = new LongIdGenerator<T>();

        private LongIdGenerator()
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
        internal void Release(long id)
        {
            _usedIds.Enqueue(id);
        }
    }
}
