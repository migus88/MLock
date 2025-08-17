using System;
using System.Collections.Generic;

namespace Migs.MLock.Interfaces
{
    public interface ILockableData<TLockTags> where TLockTags : Enum
    {
        ILockable<TLockTags> Lockable { get; }
        HashSet<ILock<TLockTags>> Locks { get; }
        bool IsLocked { get; }

        void Init(ILockable<TLockTags> lockable);
        void Reset();
    }
}