using System;
using System.Collections.Generic;

namespace CBT.Core.UnitTests
{
    /// <summary>
    /// A class that allows lambda expressions instead of having to implement <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equals;

        public LambdaComparer(Func<T, T, bool> equals)
        {
            _equals = equals;
        }

        public bool Equals(T x, T y) => _equals(x, y);

        public int GetHashCode(T obj) => 0;
    }
}