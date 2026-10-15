// The file uses `.Designer` postfix for nice file nesting in Rider

using System.Collections.Generic;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Storage;

namespace Migs.MLock
{
    public partial class BaseLockService<TLockTags> : IDebuggableLockService
    {
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
            for (var i = _locks.Count - 1; i >= 0; i--)
            {
                if (i >= _locks.Count)
                {
                    continue;
                }

                var slot = _locks.SlotAt(i);
                ReleaseLock(slot, _locks.GenerationOf(slot));
            }
        }
    }
}
