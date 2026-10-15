#nullable enable
// The file uses `.Designer` postfix for nice file nesting in Rider

using System;
using System.Collections.Generic;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Storage;

namespace Migs.MLock
{
    public partial class BaseLockService<TLockTags> : IDebuggableLockService
    {
        private SlotRef[]? _unlockAllBuffer;
        private bool _isUnlockingAll;

        public void PopulateDebugInfo(List<LockDebugInfo> debugInfo)
        {
            for (var i = 0; i < _locks.Count; i++)
            {
                ref var entry = ref _locks.Get(_locks.SlotAt(i));
                var affected = new List<string>();

                for (var j = 0; j < _subscribers.Count; j++)
                {
                    ref var subscriber = ref _subscribers.Get(_subscribers.SlotAt(j));

                    if (TagMask.Matches(entry.Mode, entry.Mask, subscriber.Tags))
                    {
                        affected.Add(DebugUtils.FormatLockable(subscriber.Lockable));
                    }
                }

                debugInfo.Add(new LockDebugInfo
                {
                    Id = entry.Id,
                    LockType = typeof(TLockTags).Name,
                    Mode = entry.Mode.ToString(),
                    IncludeTags = entry.Mode == LockMode.Include ? entry.Tags.ToString() : null,
                    ExcludeTags = entry.Mode == LockMode.Exclude ? entry.Tags.ToString() : null,
                    OriginFrames = entry.Origin != null ? new List<DebugOrigin>(entry.Origin) : null,
                    AffectedLockables = affected
                });
            }
        }

        public void UnlockById(int lockId)
        {
            for (var i = 0; i < _locks.Count; i++)
            {
                var slot = _locks.SlotAt(i);

                if (_locks.Get(slot).Id == lockId)
                {
                    ReleaseLock(slot, _locks.GenerationOf(slot));
                    return;
                }
            }
        }

        public void UnlockAll()
        {
            var count = _locks.Count;

            if (count == 0)
            {
                return;
            }

            var isNested = _isUnlockingAll;
            var snapshot = isNested ? new SlotRef[count] : RentUnlockAllBuffer(count);

            for (var i = 0; i < count; i++)
            {
                var slot = _locks.SlotAt(i);
                snapshot[i] = new SlotRef(slot, _locks.GenerationOf(slot));
            }

            Exception? first = null;
            List<Exception>? more = null;
            _isUnlockingAll = true;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        ReleaseLock(snapshot[i].Slot, snapshot[i].Generation);
                    }
                    catch (Exception e)
                    {
                        Collect(e, ref first, ref more);
                    }
                }
            }
            finally
            {
                _isUnlockingAll = isNested;
            }

            if (first != null)
            {
                Rethrow(first, more);
            }
        }

        private SlotRef[] RentUnlockAllBuffer(int count)
        {
            var buffer = _unlockAllBuffer;

            if (buffer == null || buffer.Length < count)
            {
                buffer = new SlotRef[Math.Max(count, buffer == null ? 8 : buffer.Length * 2)];
                _unlockAllBuffer = buffer;
            }

            return buffer;
        }
    }
}
