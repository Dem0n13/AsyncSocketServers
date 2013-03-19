using NLog;

namespace Dem0n13.Utils
{
    public abstract class UniqueObject<T> : IUnique<T>
    {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // ReSharper restore StaticFieldInGenericType

        private readonly int _id = IdGenerator<T>.Current.GetNext();
        protected readonly string Name;

        public int Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Initializes a new unique instance of the <see cref="UniqueObject{T}"/>
        /// </summary>
        protected UniqueObject()
        {
            Name = typeof (T).Name + "_" + _id;
            Logger.Trace("Object {0} is created", Name);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
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
            IdGenerator<T>.Current.Release(_id);
        }
    }
}
