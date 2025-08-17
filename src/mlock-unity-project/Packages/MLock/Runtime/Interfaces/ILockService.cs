using System;
using System.Collections.Generic;
using Migs.MLock.Debugging;

namespace Migs.MLock.Interfaces
{
    /// <summary>
    /// Base interface for lock services
    /// </summary>
    public interface ILockService { }

    /// <summary>
    /// Interface for a service that manages locks and lockable objects
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public interface ILockService<TLockTags> : ILockService where TLockTags : Enum
    {        
        /// <summary>
        /// Adds a lockable object to be managed by this service
        /// </summary>
        /// <param name="lockable">The lockable object to add</param>
        /// <exception cref="ArgumentNullException">Thrown if lockable is null</exception>
        void Subscribe(ILockable<TLockTags> lockable);
        
        /// <summary>
        /// Removes a lockable object from this service
        /// </summary>
        /// <param name="lockable">The lockable object to remove</param>
        /// <exception cref="ArgumentNullException">Thrown if lockable is null</exception>
        void Unsubscribe(ILockable<TLockTags> lockable);

        /// <summary>
        /// Adds a lock to this service and applies it to matching lockable objects
        /// </summary>
        /// <param name="lock">The lock to add</param>
        /// <exception cref="ArgumentNullException">Thrown if lock is null</exception>
        void TryLocking(ILock<TLockTags> @lock);

        /// <summary>
        /// Removes a lock from this service and unlocks affected lockable objects
        /// </summary>
        /// <param name="lock">The lock to remove</param>
        /// <exception cref="ArgumentNullException">Thrown if lock is null</exception>
        void TryUnlocking(ILock<TLockTags> @lock);
        
        /// <summary>
        /// Creates a lock with specific include tags
        /// </summary>
        /// <param name="includeTags">Tags to include for locking</param>
        /// <returns>A new lock instance from the pool</returns>
        ILock<TLockTags> Lock(TLockTags includeTags);
        
        /// <summary>
        /// Creates a lock that locks everything except specific tags
        /// </summary>
        /// <param name="excludeTags">Tags to exclude from locking</param>
        /// <returns>A new lock instance from the pool</returns>
        ILock<TLockTags> LockAllExcept(TLockTags excludeTags);
        
        /// <summary>
        /// Creates a lock that locks everything
        /// </summary>
        /// <returns>A new lock instance from the pool</returns>
        ILock<TLockTags> LockAll();
        
        /// <summary>
        /// Checks if a lockable is currently locked
        /// </summary>
        /// <param name="lockable">The lockable to check</param>
        /// <returns>True if the lockable is locked, false otherwise</returns>
        bool IsLocked(ILockable<TLockTags> lockable);
    }

    public interface IDebuggableLockService : ILockService
    {
        void PopulateDebugInfo(List<LockDebugInfo> debugInfo);
        bool TryUnlockById(int lockId);
        void UnlockAll();
    }
}