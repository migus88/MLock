#nullable enable
using System;
using System.Collections.Generic;
using Migs.MLock.Debugging;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// An active lock as stored by the lock service
    /// </summary>
    internal struct LockEntry<TLockTags> where TLockTags : Enum
    {
        public LockMode Mode;
        public ulong Mask;
        public TLockTags Tags;
        public int Id;
        public List<DebugOrigin>? Origin;
    }
}
