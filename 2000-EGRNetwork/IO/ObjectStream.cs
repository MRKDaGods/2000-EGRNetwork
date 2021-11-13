namespace MRK.IO
{
    public class ObjectStream<T>
    {
        private readonly T[] _array;
        private uint _position;

        public uint MaxIndex
        {
            get { return (uint)_array.Length - 1; }
        }

        public ObjectStream(T[] array)
        {
            _array = array;
            _position = 0;
        }

        public bool HasNext()
        {
            return _position < MaxIndex;
        }

        public T Read()
        {
            return _array[_position++];
        }

        public bool IsEOS()
        {
            return _position > MaxIndex;
        }

        public void Reset()
        {
            _position = 0;
        }
    }
}
