using System;
using System.Collections.Generic;
using FluentAssertions;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Migs.MLock.Tests
{
    public class LockSwitchTests
    {
        private const int SubscriberCount = 32;

        private BaseLockService<TestLockTags> _lockService;
        private RecordingLockable _tag1Lockable;
        private RecordingLockable _tag2Lockable;

        public enum SwitchKind
        {
            Include = 1,
            AllExcept = 2,
            All = 3
        }

        [SetUp]
        public void Setup()
        {
            DebugDataHandler.SetEnabled(false);

            _lockService = new BaseLockService<TestLockTags>(lockCapacity: 4, lockableCapacity: SubscriberCount * 2);
            _tag1Lockable = new RecordingLockable(TestLockTags.Tag1);
            _tag2Lockable = new RecordingLockable(TestLockTags.Tag2);
            _lockService.Subscribe(_tag1Lockable);
            _lockService.Subscribe(_tag2Lockable);
        }

        [Test]
        public void Create_StartsOff()
        {
            // Act
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);

            // Assert
            lockSwitch.IsOn.Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
        }

        [Test]
        public void SetTrue_Repeated_HoldsOneLock()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);

            // Act
            lockSwitch.Set(true);
            lockSwitch.Set(true);
            lockSwitch.Set(true);
            var lockCallsWhileOn = _tag1Lockable.LockCalls;
            lockSwitch.Set(false);

            // Assert
            lockCallsWhileOn.Should().Be(1);
            lockSwitch.IsOn.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
        }

        [TestCase(SwitchKind.Include, true, false)]
        [TestCase(SwitchKind.AllExcept, false, true)]
        [TestCase(SwitchKind.All, true, true)]
        public void SetFalse_UnlocksMatchingLockables(SwitchKind kind, bool isTag1Locked, bool isTag2Locked)
        {
            // Arrange
            var lockSwitch = CreateSwitch(kind);
            lockSwitch.Set(true);
            var tag1LockedWhileOn = _lockService.IsLocked(_tag1Lockable);
            var tag2LockedWhileOn = _lockService.IsLocked(_tag2Lockable);

            // Act
            lockSwitch.Set(false);

            // Assert
            tag1LockedWhileOn.Should().Be(isTag1Locked);
            tag2LockedWhileOn.Should().Be(isTag2Locked);
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _lockService.IsLocked(_tag2Lockable).Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
            _tag2Lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void SetFalse_WhenOff_DoesNothing()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            var otherLock = _lockService.Lock(TestLockTags.Tag1);

            // Act
            lockSwitch.Set(false);

            // Assert
            _tag1Lockable.UnlockCalls.Should().Be(0);
            otherLock.IsActive.Should().BeTrue();
            _lockService.IsLocked(_tag1Lockable).Should().BeTrue();

            otherLock.Dispose();
        }

        [Test]
        public void SetTrue_AfterRelease_ReArms()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            lockSwitch.Set(true);
            lockSwitch.Set(false);

            // Act
            lockSwitch.Set(true);

            // Assert
            lockSwitch.IsOn.Should().BeTrue();
            _tag1Lockable.LockCalls.Should().Be(2);
            _lockService.IsLocked(_tag1Lockable).Should().BeTrue();

            lockSwitch.Dispose();
        }

        [Test]
        public void Dispose_WhileOn_Unlocks()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            lockSwitch.Set(true);

            // Act
            lockSwitch.Dispose();

            // Assert
            lockSwitch.IsOn.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void Set_AfterDispose_DoesNothing()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            lockSwitch.Dispose();

            // Act
            Action act = () => lockSwitch.Set(true);

            // Assert
            act.Should().NotThrow();
            lockSwitch.IsOn.Should().BeFalse();
            _tag1Lockable.LockCalls.Should().Be(0);
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
        }

        [Test]
        public void Dispose_Twice_DoesNotReleaseOtherLock()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            lockSwitch.Set(true);
            lockSwitch.Dispose();
            var otherLock = _lockService.Lock(TestLockTags.Tag1);

            // Act
            lockSwitch.Dispose();

            // Assert
            otherLock.IsActive.Should().BeTrue();
            _lockService.IsLocked(_tag1Lockable).Should().BeTrue();

            otherLock.Dispose();
        }

        [Test]
        public void SetFalse_FromLockingCallback_EndsOffWithNoLockHeld()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            _tag1Lockable.OnLocking = () => lockSwitch.Set(false);

            // Act
            lockSwitch.Set(true);

            // Assert
            lockSwitch.IsOn.Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
        }

        [Test]
        public void Set_WhenCallbackTurnsSwitchOffAndOnDuringAcquire_KeepsExactlyOneLock()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            _tag1Lockable.OnLocking = () =>
            {
                _tag1Lockable.OnLocking = null;
                lockSwitch.Set(false);
                lockSwitch.Set(true);
            };
            var debugInfo = new List<LockDebugInfo>();

            // Act
            lockSwitch.Set(true);
            _lockService.PopulateDebugInfo(debugInfo);
            var activeLocksWhileOn = debugInfo.Count;
            lockSwitch.Set(false);

            // Assert
            activeLocksWhileOn.Should().Be(1);
            lockSwitch.IsOn.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void Set_WhenCallbackSetsTrueAgainDuringAcquire_KeepsTheLock()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            _tag1Lockable.OnLocking = () => lockSwitch.Set(true);

            // Act
            lockSwitch.Set(true);
            var isLockedWhileOn = _lockService.IsLocked(_tag1Lockable);
            lockSwitch.Set(false);

            // Assert
            isLockedWhileOn.Should().BeTrue();
            lockSwitch.IsOn.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _tag1Lockable.IsLocked.Should().BeFalse();
        }

        [Test]
        public void SetTrue_WhenLockingCallbackThrows_RethrowsAndStaysOff()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            _tag1Lockable.OnLocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => lockSwitch.Set(true);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("callback failure");
            lockSwitch.IsOn.Should().BeFalse();
            _lockService.IsLocked(_tag1Lockable).Should().BeFalse();
            _tag1Lockable.OnLocking = null;
            lockSwitch.Set(true);
            _lockService.IsLocked(_tag1Lockable).Should().BeTrue();
            lockSwitch.Dispose();
        }

        [TestCase(SwitchKind.Include)]
        [TestCase(SwitchKind.AllExcept)]
        [TestCase(SwitchKind.All)]
        public void SetToggle_AfterWarmUp_DoesNotAllocate(SwitchKind kind)
        {
            // Arrange
            for (var i = 0; i < SubscriberCount; i++)
            {
                var tags = i % 2 == 0 ? TestLockTags.Tag1 : TestLockTags.Tag3;
                _lockService.Subscribe(new RecordingLockable(tags));
            }

            var lockSwitch = CreateSwitch(kind);
            TestDelegate toggle = () =>
            {
                lockSwitch.Set(true);
                lockSwitch.Set(false);
            };
            toggle();

            // Act & Assert
            Assert.That(toggle, Is.Not.AllocatingGCMemory());
        }

        private LockSwitch<TestLockTags> CreateSwitch(SwitchKind kind)
        {
            switch (kind)
            {
                case SwitchKind.Include:
                    return _lockService.CreateSwitch(TestLockTags.Tag1);
                case SwitchKind.AllExcept:
                    return _lockService.CreateSwitchAllExcept(TestLockTags.Tag1);
                case SwitchKind.All:
                    return _lockService.CreateSwitchAll();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private sealed class RecordingLockable : ILockable<TestLockTags>
        {
            public TestLockTags LockTags { get; }
            public int LockCalls { get; private set; }
            public int UnlockCalls { get; private set; }
            public bool IsLocked { get; private set; }
            public Action OnLocking { get; set; }

            public RecordingLockable(TestLockTags tags)
            {
                LockTags = tags;
            }

            public void HandleLocking()
            {
                LockCalls++;
                IsLocked = true;
                OnLocking?.Invoke();
            }

            public void HandleUnlocking()
            {
                UnlockCalls++;
                IsLocked = false;
            }
        }
    }
}
