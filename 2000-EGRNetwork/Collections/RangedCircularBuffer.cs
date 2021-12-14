namespace MRK.Collections
{
    public class RangedCircularBuffer
    {
        private readonly float[] _array;
        private int _head;
        private int _tail;

        public float Current
        {
            get { return _array[_tail]; }
        }

        public RangedCircularBuffer(int capacity)
        {
            _array = new float[capacity];
            _head = _tail = 0;
        }

        public void Add(float val)
        {
            _array[_head] = val;
            _tail = _head;
            _head = (_head + 1) % _array.Length;
        }

        public float Density(float reference, float maxDiff)
        {
            //range / count
            float minAllowed = reference - maxDiff;
            int count = 0;
            foreach (float f in _array)
            {
                if (f < minAllowed) continue;
                count++;
            }

            //let maxDiff = 10 (so in the last 10secs)
            // if we have 100 values in last 10s, density = 100/10=10r per s
            return count / maxDiff;
        }
    }
}
