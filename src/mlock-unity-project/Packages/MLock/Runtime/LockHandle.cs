#nullable enable
using System;

namespace Migs.MLock
{
    /// <summary>
    /// A handle to an active lock. Disposing it releases the lock.
    /// Disposing a released handle, a stale copy or a <c>default</c> handle does nothing,
    /// so a handle can never release a newer lock.
    /// Store it as a <see cref="LockHandle{TLockTags}"/> field, not as <see cref="IDisposable"/>, which boxes it.
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public readonly struct LockHandle<TLockTags> : IDisposable where TLockTags : Enum
    {
        private readonly BaseLockService<TLockTags>? _service;
        private readonly int _slot;
        private readonly int _generation;

        /// <summary>
        /// Whether the lock this handle refers to is still active
        /// </summary>
        public bool IsActive => _service != null && _service.IsLockActive(_slot, _generation);

        internal LockHandle(BaseLockService<TLockTags> service, int slot, int generation)
        {
            _service = service;
            _slot = slot;
            _generation = generation;
        }

        /// <summary>
        /// Releases the lock if it is still active
        /// </summary>
        public void Dispose()
        {
            _service?.ReleaseLock(_slot, _generation);
        }
    }
}
