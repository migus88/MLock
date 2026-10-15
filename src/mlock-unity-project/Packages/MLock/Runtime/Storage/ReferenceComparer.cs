#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// Compares instances by reference, ignoring any overridden <see cref="object.Equals(object)"/> or
    /// <see cref="object.GetHashCode"/>, so two distinct objects that compare equal are never treated as one
    /// </summary>
    /// <typeparam name="T">The compared reference type</typeparam>
    internal sealed class ReferenceComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

        private ReferenceComparer()
        {
        }

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
