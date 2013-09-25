using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MMBot
{
    public class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;
        private readonly Func<T, int> _hashCode;

        public GenericEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hashCode)
        {
            _comparer = comparer;
            _hashCode = hashCode;
        }

        public bool Equals(T x, T y)
        {
            return _comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _hashCode(obj);
        }
    }
}