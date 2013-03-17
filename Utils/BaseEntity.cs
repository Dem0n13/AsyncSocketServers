using NLog;

namespace Dem0n13.Utils
{
    public class BaseEntity<T>
    {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // ReSharper restore StaticFieldInGenericType

        public readonly int Id = IntIdGenerator<T>.Current.GetNext();
        protected readonly string Name;

        /// <summary>
        /// Initializes a new unique instance of the <see cref="BaseEntity{T}"/>
        /// </summary>
        public BaseEntity()
        {
            Name = typeof(T).Name + "_" + Id;
            Logger.Trace("Entity {0} is created", Name);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override string ToString()
        {
            return Name;
        }

        ~BaseEntity()
        {
            Logger.Trace("Entity {0} is deleted", Name);
            IntIdGenerator<T>.Current.Release(Id);
        }
    }
}
