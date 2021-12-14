using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MRK.Threading
{
    public class InterlockedReferenceComparer<T> : IEqualityComparer<InterlockedReference<T>>
    {
        public bool Equals(InterlockedReference<T> x, InterlockedReference<T> y)
        {
            if (x == null || y == null) return false;
            if (x.Value == null || y.Value == null) return false;

            return x.Value.Equals(y.Value);
        }

        public int GetHashCode([DisallowNull] InterlockedReference<T> obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
