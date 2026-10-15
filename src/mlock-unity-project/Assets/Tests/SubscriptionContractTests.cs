using FluentAssertions;
using Migs.MLock.Interfaces;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class SubscriptionContractTests
    {
        private BaseLockService<TestLockTags> _lockService;

        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>();
        }

        [Test]
        public void Unsubscribe_WhenNotLocked_DoesNotCallHandleUnlocking()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            _lockService.Lock(TestLockTags.Tag2);

            // Act
            _lockService.Unsubscribe(lockable);

            // Assert
            lockable.UnlockCalls.Should().Be(0);
        }

        [Test]
        public void Unsubscribe_WhenLocked_CallsHandleUnlockingOnce()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            _lockService.Lock(TestLockTags.Tag1);
            _lockService.LockAll();

            // Act
            _lockService.Unsubscribe(lockable);

            // Assert
            lockable.UnlockCalls.Should().Be(1);
            lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void Dispose_AfterUnsubscribe_DoesNotNotifyLockable()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            _lockService.Unsubscribe(lockable);

            // Act
            handle.Dispose();

            // Assert
            lockable.LockCalls.Should().Be(1);
            lockable.UnlockCalls.Should().Be(1);
        }

        [Test]
        public void LockTagsChangedAfterSubscribe_NewLockOnNewTag_DoesNotLock()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            lockable.LockTags = TestLockTags.Tag2;

            // Act
            _lockService.Lock(TestLockTags.Tag2);

            // Assert
            _lockService.IsLocked(lockable).Should().BeFalse();
            lockable.LockCalls.Should().Be(0);
        }

        [Test]
        public void LockTagsChangedWhileLocked_Dispose_StillUnlocksOnce()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            lockable.LockTags = TestLockTags.Tag2;

            // Act
            handle.Dispose();

            // Assert
            _lockService.IsLocked(lockable).Should().BeFalse();
            lockable.IsLocked.Should().BeFalse();
            lockable.UnlockCalls.Should().Be(1);
        }

        [Test]
        public void Resubscribe_AfterTagChange_UsesNewTags()
        {
            // Arrange
            var lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(lockable);
            lockable.LockTags = TestLockTags.Tag2;
            _lockService.Unsubscribe(lockable);
            _lockService.Subscribe(lockable);

            // Act
            _lockService.Lock(TestLockTags.Tag2);

            // Assert
            _lockService.IsLocked(lockable).Should().BeTrue();
        }

        [Test]
        public void Unsubscribe_MiddleSubscriber_KeepsOtherLockStates()
        {
            // Arrange
            var first = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            var middle = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            var last = new CountingLockable<TestLockTags>(TestLockTags.Tag2);
            _lockService.Subscribe(first);
            _lockService.Subscribe(middle);
            _lockService.Subscribe(last);
            var tag1Lock = _lockService.Lock(TestLockTags.Tag1);

            // Act
            _lockService.Unsubscribe(middle);

            // Assert
            _lockService.IsLocked(first).Should().BeTrue();
            _lockService.IsLocked(last).Should().BeFalse();

            var tag2Lock = _lockService.Lock(TestLockTags.Tag2);
            tag1Lock.Dispose();
            _lockService.IsLocked(first).Should().BeFalse();
            _lockService.IsLocked(last).Should().BeTrue();
            first.UnlockCalls.Should().Be(1);
            last.LockCalls.Should().Be(1);

            tag2Lock.Dispose();
        }

        [Test]
        public void Subscribe_TwoDistinctLockablesThatAreEqual_BothSubscribe()
        {
            // Arrange
            var first = new AlwaysEqualLockable(TestLockTags.Tag1);
            var second = new AlwaysEqualLockable(TestLockTags.Tag1);

            // Act
            _lockService.Subscribe(first);
            _lockService.Subscribe(second);
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            first.LockCalls.Should().Be(1);
            second.LockCalls.Should().Be(1);
            _lockService.IsLocked(first).Should().BeTrue();
            _lockService.IsLocked(second).Should().BeTrue();

            handle.Dispose();
        }

        [Test]
        public void Unsubscribe_EqualButDistinctLockable_DoesNotRemoveTheOther()
        {
            // Arrange
            var subscribed = new AlwaysEqualLockable(TestLockTags.Tag1);
            var stranger = new AlwaysEqualLockable(TestLockTags.Tag1);
            _lockService.Subscribe(subscribed);

            // Act
            _lockService.Unsubscribe(stranger);
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            _lockService.IsLocked(subscribed).Should().BeTrue();
            _lockService.IsLocked(stranger).Should().BeFalse();
            subscribed.LockCalls.Should().Be(1);
            stranger.LockCalls.Should().Be(0);

            handle.Dispose();
        }

        private sealed class AlwaysEqualLockable : ILockable<TestLockTags>
        {
            public TestLockTags LockTags { get; }
            public int LockCalls { get; private set; }

            public AlwaysEqualLockable(TestLockTags tags)
            {
                LockTags = tags;
            }

            public void HandleLocking()
            {
                LockCalls++;
            }

            public void HandleUnlocking()
            {
            }

            public override bool Equals(object obj)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }
    }
}
