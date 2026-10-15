using System;
using System.Collections.Generic;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Storage;

namespace Migs.MLock
{
    /// <summary>
    /// A base implementation of <see cref="ILockService{TLockTags}"/>.
    /// Locks and subscribers live in pre-sized slot tables, so locking, unlocking, subscribing
    /// and unsubscribing allocate nothing once the tables are large enough.
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public partial class BaseLockService<TLockTags> : ILockService<TLockTags>
        where TLockTags : Enum
    {
        private readonly SlotTable<LockEntry<TLockTags>> _locks;
        private readonly SlotTable<SubscriberEntry<TLockTags>> _subscribers;
        private readonly Dictionary<ILockable<TLockTags>, (int Slot, int Generation)> _subscriberSlots;

        /// <summary>
        /// Creates a new lock service
        /// </summary>
        /// <param name="lockCapacity">Optional: number of simultaneously active locks to reserve space for</param>
        /// <param name="lockableCapacity">Optional: number of subscribed lockables to reserve space for</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if a capacity is negative</exception>
        public BaseLockService(int lockCapacity = 8, int lockableCapacity = 32)
        {
            if (lockCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lockCapacity), "Capacity cannot be negative");
            }

            if (lockableCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lockableCapacity), "Capacity cannot be negative");
            }

            _locks = new SlotTable<LockEntry<TLockTags>>(lockCapacity);
            _subscribers = new SlotTable<SubscriberEntry<TLockTags>>(lockableCapacity);
            _subscriberSlots = new Dictionary<ILockable<TLockTags>, (int Slot, int Generation)>(lockableCapacity);
        }

        /// <inheritdoc />
        public LockSubscription<TLockTags> Subscribe(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            if (_subscriberSlots.ContainsKey(lockable))
            {
                throw new ArgumentException($"Lockable {lockable} is already subscribed");
            }

            var tags = TagMask<TLockTags>.From(lockable.LockTags);
            var lockCount = 0;

            for (var i = 0; i < _locks.Count; i++)
            {
                ref var entry = ref _locks.Get(_locks.SlotAt(i));

                if (TagMask.Matches(entry.Mode, entry.Mask, tags))
                {
                    lockCount++;
                }
            }

            var slot = _subscribers.Add(new SubscriberEntry<TLockTags>
            {
                Lockable = lockable,
                Tags = tags,
                LockCount = lockCount
            }, out var generation);

            _subscriberSlots.Add(lockable, (slot, generation));

            if (lockCount > 0)
            {
                lockable.HandleLocking();
            }

            return new LockSubscription<TLockTags>(this, slot, generation);
        }

        /// <inheritdoc />
        public void Unsubscribe(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            if (!_subscriberSlots.TryGetValue(lockable, out var subscription))
            {
                return;
            }

            RemoveSubscriber(subscription.Slot, subscription.Generation);
        }

        /// <inheritdoc />
        public LockHandle<TLockTags> Lock(TLockTags includeTags)
        {
            return Acquire(LockMode.Include, TagMask<TLockTags>.From(includeTags), includeTags);
        }

        /// <inheritdoc />
        public LockHandle<TLockTags> LockAllExcept(TLockTags excludeTags)
        {
            return Acquire(LockMode.Exclude, TagMask<TLockTags>.From(excludeTags), excludeTags);
        }

        /// <inheritdoc />
        public LockHandle<TLockTags> LockAll()
        {
            return Acquire(LockMode.All, 0, default);
        }

        /// <inheritdoc />
        public bool IsLocked(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            return _subscriberSlots.TryGetValue(lockable, out var subscription) && _subscribers.Get(subscription.Slot).LockCount > 0;
        }

        internal bool IsSubscriptionActive(int slot, int generation)
        {
            return _subscribers.IsAlive(slot, generation);
        }

        internal void ReleaseSubscription(int slot, int generation)
        {
            RemoveSubscriber(slot, generation);
        }

        internal bool IsLockActive(int slot, int generation)
        {
            return _locks.IsAlive(slot, generation);
        }

        internal void ReleaseLock(int slot, int generation)
        {
            if (!_locks.TryRemove(slot, generation, out var entry))
            {
                return;
            }

            for (var i = 0; i < _subscribers.Count; i++)
            {
                ref var subscriber = ref _subscribers.Get(_subscribers.SlotAt(i));

                if (subscriber.LockCount == 0 || !TagMask.Matches(entry.Mode, entry.Mask, subscriber.Tags))
                {
                    continue;
                }

                subscriber.LockCount--;

                if (subscriber.LockCount == 0)
                {
                    subscriber.Lockable.HandleUnlocking();
                }
            }
        }

        private void RemoveSubscriber(int slot, int generation)
        {
            if (!_subscribers.TryRemove(slot, generation, out var removed))
            {
                return;
            }

            _subscriberSlots.Remove(removed.Lockable);

            if (removed.LockCount > 0)
            {
                removed.Lockable.HandleUnlocking();
            }
        }

        private LockHandle<TLockTags> Acquire(LockMode mode, ulong mask, TLockTags tags)
        {
            var slot = _locks.Add(new LockEntry<TLockTags>
            {
                Mode = mode,
                Mask = mask,
                Tags = tags,
                Id = LockIdGenerator.Next(),
                Origin = DebugDataHandler.IsEnabled ? DebugUtils.CaptureOrigin() : null
            }, out var generation);

            for (var i = 0; i < _subscribers.Count; i++)
            {
                ref var subscriber = ref _subscribers.Get(_subscribers.SlotAt(i));

                if (!TagMask.Matches(mode, mask, subscriber.Tags))
                {
                    continue;
                }

                subscriber.LockCount++;

                if (subscriber.LockCount == 1)
                {
                    subscriber.Lockable.HandleLocking();
                }
            }

            return new LockHandle<TLockTags>(this, slot, generation);
        }
    }
}
