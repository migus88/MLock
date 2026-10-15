#nullable enable
using System;

namespace Migs.MLock.Interfaces
{
    /// <summary>
    /// Interface for objects that can be locked by locks
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public interface ILockable<TLockTags> where TLockTags : Enum
    {
        /// <summary>
        /// The tags associated with this lockable object
        /// </summary>
        TLockTags LockTags { get; }

        /// <summary>
        /// Locks this object
        /// </summary>
        void HandleLocking();
        
        /// <summary>
        /// Unlocks this object
        /// </summary>
        void HandleUnlocking();
    }
}