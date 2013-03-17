using NLog;

namespace Dem0n13.Utils
{
    public class UniqueObject<T>
    {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // ReSharper restore StaticFieldInGenericType

        public readonly long Id = IdGenerator<T>.Current.GetNext();
        protected readonly string Name;

        /// <summary>
        /// Initializes a new unique instance of the <see cref="UniqueObject{T}"/>
        /// </summary>
        public UniqueObject()
        {
            Name = typeof(T).Name + "_" + Id;
            Logger.Trace("Object {0} is created", Name);
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

        ~UniqueObject()
        {
            Logger.Trace("Object {0} is deleted", Name);
            IdGenerator<T>.Current.Release(Id);
        }
    }
}
