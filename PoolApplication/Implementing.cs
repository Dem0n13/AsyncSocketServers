using System;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace PoolApplication
{
    public class Implementing : SocketAsyncEventArgs, IPoolable<Implementing>
    {
        private readonly PoolToken<Implementing> _poolToken;
        public PoolToken<Implementing> PoolToken { get { return _poolToken; } }

        public Implementing()
            : this(null)
        {
        }

        public Implementing(Pool<Implementing> pool)
        {
            _poolToken = new PoolToken<Implementing>(this, pool);
        }
    }

    public class ImplementingPool : Pool<Implementing>
    {
        private readonly int _bufferSize;
        
        public ImplementingPool(int bufferSize, int initialCount, int maxCapacity)
            : base(maxCapacity, PoolReleasingMethod.Manual)
        {
            _bufferSize = bufferSize;
            TryAllocatePush(initialCount);
        }

        protected override Implementing ObjectConstructor()
        {
            var item = new Implementing(this);
            item.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            return item;
        }

        protected override void CleanUp(Implementing item)
        {
            Array.Clear(item.Buffer, 0, _bufferSize);
        }
    }
}