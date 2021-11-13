namespace MRK {
    public class Reference<T> {
        public T Value { get; set; }

        public Reference() : this(default)
        {
        }

        public Reference(T defaultValue)
        {
            Value = defaultValue;
        }
    }
}
