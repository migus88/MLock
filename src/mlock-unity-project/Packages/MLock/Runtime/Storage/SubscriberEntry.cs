#nullable enable
using System;
using Migs.MLock.Interfaces;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// A subscribed lockable with its tags, read once at subscription, and the number of locks applied to it
    /// </summary>
    internal struct SubscriberEntry<TLockTags> where TLockTags : Enum
    {
        public ILockable<TLockTags> Lockable;
        public ulong Tags;
        public int LockCount;
    }
}
