#nullable enable
using System;
using Migs.MLock.Interfaces;
using Migs.MLock.Storage;

namespace Migs.MLock
{
    /// <summary>
    /// A lock driven by a boolean state: <see cref="Set"/> with <c>true</c> takes the lock once,
    /// with <c>false</c> releases it. Create it once (see <see cref="LockSwitchExtensions"/>), store it in a field
    /// and toggle it as often as needed; toggling allocates nothing.
    /// <see cref="IsOn"/> is the requested state: if the debug tools force-release the lock, the switch still
    /// reports on until the next <c>Set(false)</c>, which is then safe.
    /// <see cref="Set"/> may be called from lockable callbacks, including during the switch's own
    /// <c>Set(true)</c>: the last call wins and the switch never holds more than one lock.
    /// If taking the lock throws, the switch stays off and the exception propagates.
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public sealed class LockSwitch<TLockTags> : IDisposable where TLockTags : Enum
    {
        private readonly ILockService<TLockTags> _service;
        private readonly LockMode _mode;
        private readonly TLockTags _tags;

        private LockHandle<TLockTags> _handle;
        private bool _isOn;
        private bool _isDisposed;
        private int _version;

        /// <summary>
        /// Whether the switch is on, i.e. its lock was requested and not released
        /// </summary>
        public bool IsOn => _isOn;

        internal LockSwitch(ILockService<TLockTags> service, LockMode mode, TLockTags tags)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _mode = mode;
            _tags = tags;
        }

        /// <summary>
        /// Takes the lock when <paramref name="isOn"/> is true and releases it when false.
        /// Setting the current state again does nothing, so the switch never holds more than one lock.
        /// Does nothing after <see cref="Dispose"/>.
        /// </summary>
        /// <param name="isOn">Whether the lock should be held</param>
        public void Set(bool isOn)
        {
            if (isOn)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        /// <summary>
        /// Releases the lock if it is held and turns the switch off for good. Disposing twice is safe.
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
            TurnOff();
        }

        private void TurnOn()
        {
            if (_isDisposed || _isOn)
            {
                return;
            }

            _isOn = true;
            _version = unchecked(_version + 1);
            var version = _version;
            LockHandle<TLockTags> handle;

            try
            {
                handle = Acquire();
            }
            catch
            {
                if (version == _version)
                {
                    _isOn = false;
                }

                throw;
            }

            if (version != _version || _isDisposed)
            {
                handle.Dispose();
                return;
            }

            _handle = handle;
        }

        private void TurnOff()
        {
            if (!_isOn)
            {
                return;
            }

            _isOn = false;
            _version = unchecked(_version + 1);
            var handle = _handle;
            _handle = default;
            handle.Dispose();
        }

        private LockHandle<TLockTags> Acquire()
        {
            switch (_mode)
            {
                case LockMode.Include:
                    return _service.Lock(_tags);
                case LockMode.Exclude:
                    return _service.LockAllExcept(_tags);
                case LockMode.All:
                    return _service.LockAll();
                default:
                    throw new InvalidOperationException($"Unsupported lock mode {_mode}");
            }
        }
    }
}
