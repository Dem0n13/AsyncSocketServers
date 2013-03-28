using System;
using Dem0n13.Utils;

namespace PoolApplication
{
    public class Deriving : PoolObject<Deriving>
    {
        public byte[] Buffer;

        public Deriving()
            : this(null)
        {
        }

        public Deriving(Pool<Deriving> pool)
            : base(pool)
        {
        }
    }

    public class DerivingPool : Pool<Deriving>
    {
        public DerivingPool(int maxCapacity, PoolReleasingMethod releasingMethod)
            : base(maxCapacity, releasingMethod)
        {
        }

        protected override Deriving ObjectConstructor()
        {
            return new Deriving(this) { Buffer = new byte[10240] };
        }

        protected override void CleanUp(Deriving item)
        {
            Array.Clear(item.Buffer, 0, item.Buffer.Length);
        }
    }
}