using System;
using Migs.MLock.Interfaces;

namespace Migs.MLock.Tests.Integration
{
    /// <summary>
    /// Hand-written lockable that counts the notifications it receives without allocating
    /// </summary>
    public class CountingLockable<TLockTags> : ILockable<TLockTags> where TLockTags : Enum
    {
        private readonly string _name;

        public TLockTags LockTags { get; set; }
        public int LockCalls { get; private set; }
        public int UnlockCalls { get; private set; }
        public bool IsLocked { get; private set; }

        public CountingLockable(TLockTags tags, string name = "Lockable")
        {
            LockTags = tags;
            _name = name;
        }

        public void HandleLocking()
        {
            LockCalls++;
            IsLocked = true;
        }

        public void HandleUnlocking()
        {
            UnlockCalls++;
            IsLocked = false;
        }

        public override string ToString() => _name;
    }
}
