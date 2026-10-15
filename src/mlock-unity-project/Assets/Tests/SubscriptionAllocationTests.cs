using System;
using Migs.MLock.Debugging;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Migs.MLock.Tests
{
    public class SubscriptionAllocationTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private CountingLockable<TestLockTags> _extraLockable;
        private LockGate<TestLockTags> _gate;
        private Action<bool> _onChanged;
        private int _changeCount;

        [SetUp]
        public void Setup()
        {
            DebugDataHandler.SetEnabled(false);

            _lockService = new BaseLockService<TestLockTags>(lockCapacity: 8, lockableCapacity: 8);
            _extraLockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _onChanged = OnChanged;
            _gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _onChanged);
        }

        [TearDown]
        public void TearDown()
        {
            _gate.Dispose();
        }

        [Test]
        public void Subscribe_ThenDisposeSubscription_DoesNotAllocate()
        {
            // Arrange
            var tag1Lock = _lockService.Lock(TestLockTags.Tag1);
            var allLock = _lockService.LockAll();
            TestDelegate act = () =>
            {
                var subscription = _lockService.Subscribe(_extraLockable);
                subscription.Dispose();
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());

            tag1Lock.Dispose();
            allLock.Dispose();
        }

        [Test]
        public void LockAndRelease_WithGateSubscribed_DoesNotAllocate()
        {
            // Arrange
            TestDelegate act = () =>
            {
                var handle = _lockService.Lock(TestLockTags.Tag1);
                handle.Dispose();
            };
            act();
            var warmUpChanges = _changeCount;

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());
            Assert.That(_changeCount, Is.GreaterThan(warmUpChanges));
        }

        private void OnChanged(bool isLocked)
        {
            _changeCount++;
        }
    }
}
