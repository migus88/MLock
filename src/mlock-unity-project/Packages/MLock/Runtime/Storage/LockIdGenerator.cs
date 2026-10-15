#nullable enable
using System.Threading;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// Hands out lock ids that are unique across all lock services, for debugging
    /// </summary>
    internal static class LockIdGenerator
    {
        private static int _counter;

        public static int Next() => Interlocked.Increment(ref _counter);
    }
}
