#nullable enable
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
    /// <remarks>
    /// <see cref="Subscribe"/>, <see cref="Unsubscribe"/>, the <c>Lock</c> methods and disposing a handle or a subscription
    /// may be called from inside <see cref="ILockable{TLockTags}.HandleLocking"/> and <see cref="ILockable{TLockTags}.HandleUnlocking"/>.
    /// Each subscription receives alternating <c>HandleLocking</c>/<c>HandleUnlocking</c> calls, and the last call matches the final state.
    /// A lockable subscribed from inside a callback is notified by its own <see cref="Subscribe"/>;
    /// a lockable unsubscribed before its pending notification is delivered receives no call.
    /// <see cref="IsLocked"/> returns the current state even while notifications are still pending.
    /// If a callback throws, the exception propagates and the service stays usable; lockables whose notification
    /// was not delivered yet are notified at their next change.
    /// </remarks>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public interface ILockService<TLockTags> : ILockService where TLockTags : Enum
    {
        /// <summary>
        /// Adds a lockable object to be managed by this service.
        /// The lockable's <see cref="ILockable{TLockTags}.LockTags"/> are read once, here, and must not change
        /// while it is subscribed; to change them, unsubscribe and subscribe again.
        /// If active locks match the lockable, <see cref="ILockable{TLockTags}.HandleLocking"/> is called once.
        /// </summary>
        /// <param name="lockable">The lockable object to add</param>
        /// <exception cref="ArgumentNullException">Thrown if lockable is null</exception>
        /// <returns>A subscription that unsubscribes the lockable when disposed</returns>
        /// <exception cref="ArgumentException">Thrown if lockable is already subscribed</exception>
        LockSubscription<TLockTags> Subscribe(ILockable<TLockTags> lockable);

        /// <summary>
        /// Removes a lockable object from this service.
        /// A lockable that was notified as locked receives <see cref="ILockable{TLockTags}.HandleUnlocking"/> once; any other receives nothing.
        /// Unsubscribing a lockable that is not subscribed does nothing.
        /// </summary>
        /// <param name="lockable">The lockable object to remove</param>
        /// <exception cref="ArgumentNullException">Thrown if lockable is null</exception>
        void Unsubscribe(ILockable<TLockTags> lockable);

        /// <summary>
        /// Locks every lockable that has at least one of the given tags.
        /// Locking <c>None</c> locks nothing, but still returns an active handle.
        /// </summary>
        /// <param name="includeTags">Tags to include for locking</param>
        /// <returns>A handle that releases the lock when disposed</returns>
        LockHandle<TLockTags> Lock(TLockTags includeTags);

        /// <summary>
        /// Locks every lockable that has none of the given tags.
        /// Excluding <c>None</c> locks everything.
        /// </summary>
        /// <param name="excludeTags">Tags to exclude from locking</param>
        /// <returns>A handle that releases the lock when disposed</returns>
        LockHandle<TLockTags> LockAllExcept(TLockTags excludeTags);

        /// <summary>
        /// Locks every lockable, including lockables without tags
        /// </summary>
        /// <returns>A handle that releases the lock when disposed</returns>
        LockHandle<TLockTags> LockAll();

        /// <summary>
        /// Checks if a lockable is currently locked
        /// </summary>
        /// <param name="lockable">The lockable to check</param>
        /// <returns>True if the lockable is locked, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if lockable is null</exception>
        bool IsLocked(ILockable<TLockTags> lockable);
    }

    public interface IDebuggableLockService : ILockService
    {
        void PopulateDebugInfo(List<LockDebugInfo> debugInfo);
        void UnlockById(int lockId);
        void UnlockAll();
    }
}
