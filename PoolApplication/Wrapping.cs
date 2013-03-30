using Dem0n13.Utils;

namespace PoolApplication
{
    public sealed class ThirdParty
    {
         
    }

    public class ThirdPartyPool : Pool<PoolObjectWrapper<ThirdParty>>
    {
        public ThirdPartyPool(int maxCapacity)
            : base(maxCapacity)
        {
        }

        protected override PoolObjectWrapper<ThirdParty> ObjectConstructor()
        {
            var original = new ThirdParty();
            return new PoolObjectWrapper<ThirdParty>(original);
        }
    }
}