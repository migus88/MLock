using Migs.MLock.Debugging;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Migs.MLock.Tests
{
    public class AllocationTests
    {
        private const int SubscriberCount = 32;

        private BaseLockService<TestLockTags> _lockService;
        private CountingLockable<TestLockTags> _extraLockable;

        [SetUp]
        public void Setup()
        {
            DebugDataHandler.SetEnabled(false);

            _lockService = new BaseLockService<TestLockTags>(lockCapacity: 8, lockableCapacity: SubscriberCount * 2);

            for (var i = 0; i < SubscriberCount; i++)
            {
                var tags = i % 2 == 0 ? TestLockTags.Tag1 : TestLockTags.Tag2;
                _lockService.Subscribe(new CountingLockable<TestLockTags>(tags));
            }

            _extraLockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
        }

        [Test]
        public void LockAndDispose_AfterWarmUp_DoesNotAllocate()
        {
            // Arrange
            TestDelegate act = () =>
            {
                var handle = _lockService.Lock(TestLockTags.Tag1);
                handle.Dispose();
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void LockAllExceptAndDispose_AfterWarmUp_DoesNotAllocate()
        {
            // Arrange
            TestDelegate act = () =>
            {
                var handle = _lockService.LockAllExcept(TestLockTags.Tag2);
                handle.Dispose();
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void LockAllAndDispose_AfterWarmUp_DoesNotAllocate()
        {
            // Arrange
            TestDelegate act = () =>
            {
                var handle = _lockService.LockAll();
                handle.Dispose();
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SubscribeAndUnsubscribe_WithActiveLocks_AfterWarmUp_DoesNotAllocate()
        {
            // Arrange
            var tag1Lock = _lockService.Lock(TestLockTags.Tag1);
            var allLock = _lockService.LockAll();
            TestDelegate act = () =>
            {
                _lockService.Subscribe(_extraLockable);
                _lockService.Unsubscribe(_extraLockable);
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());

            tag1Lock.Dispose();
            allLock.Dispose();
        }
    }
}
