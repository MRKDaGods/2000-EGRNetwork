namespace MRK.Threading
{
    public class InterlockedReference<T> : InterlockedAccess
    {
        public T Value
        {
            get; set;
        }

        public InterlockedReference()
        {
        }
    }
}
