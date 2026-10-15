using FluentAssertions;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class LockSubscriptionTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private CountingLockable<TestLockTags> _lockable;

        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>(lockableCapacity: 1);
            _lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
        }

        [Test]
        public void Dispose_UnsubscribesLockable()
        {
            // Arrange
            var subscription = _lockService.Subscribe(_lockable);

            // Act
            subscription.Dispose();
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            _lockable.LockCalls.Should().Be(0);
            _lockService.IsLocked(_lockable).Should().BeFalse();
            subscription.IsActive.Should().BeFalse();

            handle.Dispose();
        }

        [Test]
        public void Dispose_WhileLocked_CallsHandleUnlockingOnce()
        {
            // Arrange
            var subscription = _lockService.Subscribe(_lockable);
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Act
            subscription.Dispose();
            handle.Dispose();

            // Assert
            _lockable.UnlockCalls.Should().Be(1);
            _lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void Dispose_Twice_DoesNotUnsubscribeLockableThatReusedSlot()
        {
            // Arrange
            var other = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            var first = _lockService.Subscribe(_lockable);
            first.Dispose();
            var second = _lockService.Subscribe(other);

            // Act
            first.Dispose();
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            second.IsActive.Should().BeTrue();
            other.IsLocked.Should().BeTrue();
            _lockService.IsLocked(other).Should().BeTrue();

            handle.Dispose();
            second.Dispose();
        }

        [Test]
        public void Dispose_AfterUnsubscribeAndResubscribe_KeepsNewSubscription()
        {
            // Arrange
            var stale = _lockService.Subscribe(_lockable);
            _lockService.Unsubscribe(_lockable);
            var current = _lockService.Subscribe(_lockable);

            // Act
            stale.Dispose();

            // Assert
            current.IsActive.Should().BeTrue();
            var handle = _lockService.Lock(TestLockTags.Tag1);
            _lockable.IsLocked.Should().BeTrue();

            handle.Dispose();
            current.Dispose();
        }

        [Test]
        public void Default_DisposeDoesNothingAndIsNotActive()
        {
            // Arrange
            var active = _lockService.Subscribe(_lockable);
            LockSubscription<TestLockTags> empty = default;

            // Act
            empty.Dispose();

            // Assert
            empty.IsActive.Should().BeFalse();
            active.IsActive.Should().BeTrue();

            active.Dispose();
        }

        [Test]
        public void IsActive_AfterUnsubscribeByLockable_IsFalse()
        {
            // Arrange
            var subscription = _lockService.Subscribe(_lockable);

            // Act
            _lockService.Unsubscribe(_lockable);

            // Assert
            subscription.IsActive.Should().BeFalse();
        }
    }
}
