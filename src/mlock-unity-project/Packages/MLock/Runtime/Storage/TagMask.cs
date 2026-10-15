#nullable enable
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// Matches a lock's mask against a lockable's tags
    /// </summary>
    internal static class TagMask
    {
        /// <summary>
        /// Whether a lock with the given mode and mask applies to a lockable with the given tags
        /// </summary>
        public static bool Matches(LockMode mode, ulong mask, ulong tags)
        {
            switch (mode)
            {
                case LockMode.Include:
                    return (mask & tags) != 0;
                case LockMode.Exclude:
                    return (mask & tags) == 0;
                case LockMode.All:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Converts enum values of any underlying type to a 64-bit mask without boxing.
    /// Values are zero-extended from their own width, so negative members convert consistently.
    /// </summary>
    internal static class TagMask<TLockTags> where TLockTags : Enum
    {
        private static readonly int Size = UnsafeUtility.SizeOf(Enum.GetUnderlyingType(typeof(TLockTags)));

        public static ulong From(TLockTags value)
        {
            switch (Size)
            {
                case 1:
                    return UnsafeUtility.As<TLockTags, byte>(ref value);
                case 2:
                    return UnsafeUtility.As<TLockTags, ushort>(ref value);
                case 4:
                    return UnsafeUtility.As<TLockTags, uint>(ref value);
                default:
                    return UnsafeUtility.As<TLockTags, ulong>(ref value);
            }
        }
    }
}
