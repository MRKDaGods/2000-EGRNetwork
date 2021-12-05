namespace MRK.Services
{
    public class ServiceOperation<T, E>
    {
        public bool Finished 
        { 
            get; private set;
        }

        public T Result 
        { 
            get; private set;
        }

        public E Extra
        {
            get; protected set;
        }

        public static ServiceOperation<T, E> Failed
        {
            get
            {
                return new ServiceOperation<T, E>(false);
            }
        }

        public ServiceOperation(bool finished, T result = default)
        {
            Finished = finished;
            Result = result;
            Extra = default;
        }

        public static ServiceOperation<T, E> FailedReason(E reason)
        {
            return new ServiceOperation<T, E>(false)
            {
                Extra = reason
            };
        }
    }
}
