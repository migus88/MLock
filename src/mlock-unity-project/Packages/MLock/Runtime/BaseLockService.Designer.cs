// The file uses `.Designer` postfix for nice file nesting in Rider

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;

namespace Migs.MLock
{
    public partial class BaseLockService<TLockTags> : IDebuggableLockService
    {
        public void PopulateDebugInfo(List<LockDebugInfo> debugInfo) => debugInfo.Populate(_activeLocks, _lockableToDataMap);
        public void UnlockById(int lockId) => _activeLocks.UnlockById(lockId);
        public void UnlockAll() => _activeLocks.UnlockAll();
    }
}