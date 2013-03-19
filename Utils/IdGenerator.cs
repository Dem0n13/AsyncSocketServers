using System.Collections.Concurrent;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides the thread safely generating unique <see cref="long"/> numbers within specified type.
    /// Generation starts from zero.
    /// Automaticly reuses old numbers - only for <see cref="UniqueObject{T}"/>.
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

        private readonly ConcurrentQueue<int> _usedIds = new ConcurrentQueue<int>();
        private int _currentMaxId = -1;

        /// <summary>
        /// Generates a new unique <see cref="int"/> number
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
