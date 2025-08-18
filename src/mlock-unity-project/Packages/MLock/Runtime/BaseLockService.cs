using System;
using System.Collections.Generic;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Pools;

namespace Migs.MLock
{
    /// <summary>
    /// A base implementation of <see cref="ILockService{TLockTags}"/>
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public partial class BaseLockService<TLockTags> : ILockService<TLockTags>
        where TLockTags : Enum
    {
        private readonly Dictionary<ILockable<TLockTags>, ILockableData<TLockTags>> _lockableToDataMap = new();
        private readonly HashSet<ILock<TLockTags>> _activeLocks = new();
        private readonly ILockPool<TLockTags> _lockPool;
        private readonly ILockableDataPool<TLockTags> _dataPool;
        
        /// <summary>
        /// Creates a new lock service
        /// </summary>
        /// <param name="poolsInitialCapacity">Optional: initial capacity for internal object pools</param>
        public BaseLockService(int poolsInitialCapacity = 10)
        {
            _lockPool = new LockPool<TLockTags>(poolsInitialCapacity);
            _dataPool = new LockableDataPool<TLockTags>(poolsInitialCapacity);
        }

        /// <inheritdoc />
        public void Subscribe(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            if (_lockableToDataMap.ContainsKey(lockable))
            {
                throw new ArgumentException($"Lockable {lockable} is already subscribed");
            }

            var data = _dataPool.Borrow(lockable);
            var isLocked = false;

            // Check directly against active locks instead of iterating all lockables
            foreach (var @lock in _activeLocks)
            {
                if (ShouldLock(@lock, data))
                {
                    data.Locks.Add(@lock);
                    isLocked = true;
                }
            }
            
            _lockableToDataMap.Add(lockable, data);

            if (isLocked)
            {
                data.Lockable.HandleLocking();
            }
        }

        /// <inheritdoc />
        public void Unsubscribe(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            if (!_lockableToDataMap.TryGetValue(lockable, out var data))
            {
                return;
            }

            lockable.HandleUnlocking();
            _dataPool.Return(data);
            
            _lockableToDataMap.Remove(lockable);
        }

        /// <inheritdoc />
        public ILock<TLockTags> Lock(TLockTags includeTags)
        {
            return CreateLock(includeTags, default);
        }

        /// <inheritdoc />
        public ILock<TLockTags> LockAllExcept(TLockTags excludeTags)
        {
            return CreateLock(default, excludeTags);
        }

        /// <inheritdoc />
        public ILock<TLockTags> LockAll()
        {
            return CreateLock(default, default);
        }

        private ILock<TLockTags> CreateLock(TLockTags includeTags = default, TLockTags excludeTags = default)
        {
            // Validate that both include and exclude tags are not set
            if (!includeTags.Equals(default(TLockTags)) && !excludeTags.Equals(default(TLockTags)))
            {
                throw new ArgumentException("Cannot specify both include and exclude tags");
            }

            var @lock = _lockPool.Borrow(this, includeTags, excludeTags);
            @lock.PopulateLockOrigin();
            
            _activeLocks.Add(@lock);

            TryLocking(@lock);

            return @lock;
        }

        /// <inheritdoc />
        public void TryLocking(ILock<TLockTags> @lock)
        {
            if (@lock == null)
            {
                throw new ArgumentNullException(nameof(@lock), "Lock cannot be null");
            }
            
            _activeLocks.Add(@lock);

            foreach (var kvp in _lockableToDataMap)
            {
                var data = kvp.Value;
                
                if(!ShouldLock(@lock, data))
                {
                    continue;
                }
                
                data.Locks.Add(@lock);
                
                if(data.Locks.Count == 1)
                {
                    data.Lockable.HandleLocking();
                }
            }
        }

        /// <inheritdoc />
        public void TryUnlocking(ILock<TLockTags> @lock)
        {
            if (@lock == null)
            {
                throw new ArgumentNullException(nameof(@lock), "Lock cannot be null");
            }

            _activeLocks.Remove(@lock);

            foreach (var kvp in _lockableToDataMap)
            {
                var data = kvp.Value;

                if (!data.Locks.Contains(@lock))
                {
                    continue;
                }
                
                data.Locks.Remove(@lock);
                
                if (data.Locks.Count == 0)
                {
                    data.Lockable.HandleUnlocking();
                }
            }
        }

        /// <inheritdoc />
        public bool IsLocked(ILockable<TLockTags> lockable)
        {
            if (lockable == null)
            {
                throw new ArgumentNullException(nameof(lockable), "Lockable cannot be null");
            }

            return _lockableToDataMap.TryGetValue(lockable, out var data) && data.IsLocked;
        }
        
        private static bool ShouldLock(ILock<TLockTags> @lock, ILockableData<TLockTags> data)
        {
            if (data == null)
            {
                return false;
            }
            
            if (data.Locks.Contains(@lock))
            {
                return false;
            }

            // Perform direct bitwise operations on the enum values
            var includeTags = Convert.ToUInt32(@lock.IncludeTags);
            var excludeTags = Convert.ToUInt32(@lock.ExcludeTags);
            var lockableTags = Convert.ToUInt32(data.Lockable.LockTags);

            // If both include and exclude are 0 (default), lock everything
            if (includeTags == 0 && excludeTags == 0)
            {
                return true;
            }

            // If both include and exclude have values, that's an invalid configuration
            if (includeTags != 0 && excludeTags != 0)
            {
                return false;
            }

            // Check if any lockable tag is included in the include tags
            if (includeTags != 0)
            {
                return (includeTags & lockableTags) != 0;
            }

            // Check if no lockable tag is in the exclude tags
            if (excludeTags != 0)
            {
                return (excludeTags & lockableTags) == 0;
            }

            return false;
        }
    }
}