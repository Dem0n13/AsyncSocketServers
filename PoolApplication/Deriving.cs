using System;
using Dem0n13.Utils;

namespace PoolApplication
{
    public class Deriving : PoolObject
    {
        public byte[] Buffer;
    }

    public class DerivingPool : Pool<Deriving>
    {
        public DerivingPool(int maxCapacity)
            : base(maxCapacity)
        {
        }

        protected override Deriving ObjectConstructor()
        {
            return new Deriving { Buffer = new byte[10240] };
        }

        protected override void CleanUp(Deriving item)
        {
            Array.Clear(item.Buffer, 0, item.Buffer.Length);
        }
    }
}