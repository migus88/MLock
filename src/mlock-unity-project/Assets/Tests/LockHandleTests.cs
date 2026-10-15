using FluentAssertions;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class LockHandleTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private CountingLockable<TestLockTags> _lockable;

        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>(lockCapacity: 1);
            _lockable = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            _lockService.Subscribe(_lockable);
        }

        [Test]
        public void Dispose_Twice_DoesNotReleaseNewerLockOnReusedSlot()
        {
            // Arrange
            var first = _lockService.Lock(TestLockTags.Tag1);
            first.Dispose();
            var second = _lockService.Lock(TestLockTags.Tag1);

            // Act
            first.Dispose();

            // Assert
            second.IsActive.Should().BeTrue();
            _lockService.IsLocked(_lockable).Should().BeTrue();
            _lockable.IsLocked.Should().BeTrue();

            second.Dispose();
        }

        [Test]
        public void Dispose_StaleCopy_DoesNotReleaseNewerLock()
        {
            // Arrange
            var original = _lockService.Lock(TestLockTags.Tag1);
            var copy = original;
            original.Dispose();
            var newer = _lockService.Lock(TestLockTags.Tag1);

            // Act
            copy.Dispose();

            // Assert
            newer.IsActive.Should().BeTrue();
            _lockService.IsLocked(_lockable).Should().BeTrue();

            newer.Dispose();
        }

        [Test]
        public void Dispose_Default_DoesNothing()
        {
            // Arrange
            var active = _lockService.Lock(TestLockTags.Tag1);
            LockHandle<TestLockTags> empty = default;

            // Act
            empty.Dispose();

            // Assert
            empty.IsActive.Should().BeFalse();
            active.IsActive.Should().BeTrue();
            _lockService.IsLocked(_lockable).Should().BeTrue();

            active.Dispose();
        }

        [Test]
        public void Dispose_OnCopies_ReleasesOnce()
        {
            // Arrange
            var original = _lockService.Lock(TestLockTags.Tag1);
            var copy = original;

            // Act
            original.Dispose();
            copy.Dispose();

            // Assert
            _lockable.UnlockCalls.Should().Be(1);
            _lockService.IsLocked(_lockable).Should().BeFalse();
        }

        [Test]
        public void IsActive_AfterDispose_IsFalse()
        {
            // Arrange
            var handle = _lockService.Lock(TestLockTags.Tag1);
            handle.IsActive.Should().BeTrue();

            // Act
            handle.Dispose();

            // Assert
            handle.IsActive.Should().BeFalse();
        }

        [Test]
        public void Lock_BeyondInitialCapacity_GrowsAndReleasesEachLock()
        {
            // Arrange
            var second = new CountingLockable<TestLockTags>(TestLockTags.Tag2);
            _lockService.Subscribe(second);

            var tag1Lock = _lockService.Lock(TestLockTags.Tag1);
            var tag2Lock = _lockService.Lock(TestLockTags.Tag2);
            var bothLock = _lockService.Lock(TestLockTags.Tag1 | TestLockTags.Tag2);

            // Act & Assert
            tag2Lock.Dispose();
            second.IsLocked.Should().BeTrue();
            tag1Lock.IsActive.Should().BeTrue();
            bothLock.IsActive.Should().BeTrue();

            bothLock.Dispose();
            second.IsLocked.Should().BeFalse();
            _lockable.IsLocked.Should().BeTrue();

            tag1Lock.Dispose();
            _lockable.IsLocked.Should().BeFalse();
            _lockable.LockCalls.Should().Be(1);
            _lockable.UnlockCalls.Should().Be(1);
            second.LockCalls.Should().Be(1);
            second.UnlockCalls.Should().Be(1);
        }
    }
}
