#nullable enable
using System;
using Migs.MLock.Interfaces;

namespace Migs.MLock
{
    /// <summary>
    /// A ready-made lockable: subscribes itself on construction, tracks whether it is locked
    /// and reports each change through an optional callback. Disposing it unsubscribes it and stops notifications.
    /// Use it instead of implementing <see cref="ILockable{TLockTags}"/> by hand.
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public sealed class LockGate<TLockTags> : ILockable<TLockTags>, IDisposable where TLockTags : Enum
    {
        private readonly Action<bool>? _onChanged;
        private readonly LockSubscription<TLockTags> _subscription;
        private bool _isDisposed;

        /// <summary>
        /// Whether a matching lock is active. Always false after <see cref="Dispose"/>.
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// The tags this gate was subscribed with
        /// </summary>
        public TLockTags LockTags { get; }

        /// <summary>
        /// Creates a gate and subscribes it to the service.
        /// If a matching lock is already active, the gate starts locked and <paramref name="onChanged"/> is called with true.
        /// </summary>
        /// <param name="service">The lock service to subscribe to</param>
        /// <param name="tags">The tags this gate responds to</param>
        /// <param name="onChanged">Optional: called with the new state each time the gate locks or unlocks</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null</exception>
        public LockGate(ILockService<TLockTags> service, TLockTags tags, Action<bool>? onChanged = null)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Lock service cannot be null");
            }

            LockTags = tags;
            _onChanged = onChanged;
            _subscription = service.Subscribe(this);
        }

        /// <summary>
        /// Unsubscribes the gate. <see cref="IsLocked"/> becomes false without a callback. Disposing twice does nothing.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _subscription.Dispose();
            IsLocked = false;
        }

        void ILockable<TLockTags>.HandleLocking()
        {
            SetLocked(true);
        }

        void ILockable<TLockTags>.HandleUnlocking()
        {
            SetLocked(false);
        }

        private void SetLocked(bool isLocked)
        {
            if (_isDisposed || IsLocked == isLocked)
            {
                return;
            }

            IsLocked = isLocked;
            _onChanged?.Invoke(isLocked);
        }
    }
}
