using System;
using System.Collections.Generic;
using FluentAssertions;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class LockGateTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private List<bool> _changes;

        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>();
            _changes = new List<bool>();
        }

        [Test]
        public void Constructor_WithMatchingLockActive_StartsLockedAndCallsOnChangedTrueOnce()
        {
            // Arrange
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Act
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);

            // Assert
            gate.IsLocked.Should().BeTrue();
            _changes.Should().Equal(true);

            gate.Dispose();
            handle.Dispose();
        }

        [Test]
        public void Constructor_WithoutMatchingLock_StartsUnlockedWithoutCallback()
        {
            // Arrange
            var handle = _lockService.Lock(TestLockTags.Tag2);

            // Act
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);

            // Assert
            gate.IsLocked.Should().BeFalse();
            _changes.Should().BeEmpty();

            gate.Dispose();
            handle.Dispose();
        }

        [Test]
        public void Lock_Matching_SetsIsLockedAndCallsOnChangedOnce()
        {
            // Arrange
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            gate.IsLocked.Should().BeTrue();
            _changes.Should().Equal(true);

            handle.Dispose();
            gate.Dispose();
        }

        [Test]
        public void OverlappingLocks_CallOnChangedOncePerTransition()
        {
            // Arrange
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);
            var first = _lockService.Lock(TestLockTags.Tag1);
            var second = _lockService.LockAll();

            // Act & Assert
            first.Dispose();
            gate.IsLocked.Should().BeTrue();
            _changes.Should().Equal(true);

            second.Dispose();
            gate.IsLocked.Should().BeFalse();
            _changes.Should().Equal(true, false);

            gate.Dispose();
        }

        [Test]
        public void Dispose_StopsNotifications()
        {
            // Arrange
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);

            // Act
            gate.Dispose();
            var handle = _lockService.Lock(TestLockTags.Tag1);
            handle.Dispose();

            // Assert
            gate.IsLocked.Should().BeFalse();
            _changes.Should().BeEmpty();
        }

        [Test]
        public void Dispose_WhileLocked_SetsIsLockedFalseWithoutCallback()
        {
            // Arrange
            var gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, _changes.Add);
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Act
            gate.Dispose();

            // Assert
            gate.IsLocked.Should().BeFalse();
            _changes.Should().Equal(true);

            handle.Dispose();
            _changes.Should().Equal(true);
        }

        [Test]
        public void Constructor_NullService_Throws()
        {
            // Act
            Action act = () => new LockGate<TestLockTags>(null, TestLockTags.Tag1);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
