#nullable enable
using System;

namespace Migs.MLock
{
    /// <summary>
    /// A subscription of a lockable to a lock service. Disposing it unsubscribes the lockable.
    /// Disposing an ended subscription, a stale copy or a <c>default</c> subscription does nothing,
    /// so a subscription can never unsubscribe a newer one.
    /// Store it as a <see cref="LockSubscription{TLockTags}"/> field, not as <see cref="IDisposable"/>, which boxes it.
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public readonly struct LockSubscription<TLockTags> : IDisposable where TLockTags : Enum
    {
        private readonly BaseLockService<TLockTags>? _service;
        private readonly int _slot;
        private readonly int _generation;

        /// <summary>
        /// Whether the lockable this subscription refers to is still subscribed through it
        /// </summary>
        public bool IsActive => _service != null && _service.IsSubscriptionActive(_slot, _generation);

        internal LockSubscription(BaseLockService<TLockTags> service, int slot, int generation)
        {
            _service = service;
            _slot = slot;
            _generation = generation;
        }

        /// <summary>
        /// Unsubscribes the lockable if this subscription is still active.
        /// A locked lockable receives <see cref="Interfaces.ILockable{TLockTags}.HandleUnlocking"/> once.
        /// </summary>
        public void Dispose()
        {
            _service?.ReleaseSubscription(_slot, _generation);
        }
    }
}
