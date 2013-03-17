using System.Collections.Concurrent;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides the thread safely generating unique <see cref="long"/> numbers within specified type.
    /// Generation starts from zero.
    /// Automaticly reuses old numbers - only for <see cref="BaseEntity{T}"/>.
    /// </summary>
    public class IntIdGenerator<T>
    {
        #region Singleton implementation

        /// <summary>
        /// Current instance of <see cref="IntIdGenerator{T}"/>
        /// </summary>
        public static readonly IntIdGenerator<T> Current = new IntIdGenerator<T>();

        private IntIdGenerator()
        {
        }

        #endregion

        private readonly ConcurrentQueue<int> _usedIds = new ConcurrentQueue<int>();
        private int _currentMaxId = -1;

        /// <summary>
        /// Generates a new unique <see cref="long"/> number
        /// </summary>
        /// <returns></returns>
        public int GetNext()
        {
            int id;
            return _usedIds.TryDequeue(out id) ? id : Interlocked.Increment(ref _currentMaxId);
        }

        /// <summary>
        /// Returns unused number for future reusing
        /// </summary>
        /// <param name="id">Number to return</param>
        internal void Release(int id)
        {
            _usedIds.Enqueue(id);
        }
    }
}
