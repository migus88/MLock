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
    public class ReentrancyTests
    {
        private BaseLockService<TestLockTags> _lockService;

        [SetUp]
        public void Setup()
        {
            DebugDataHandler.SetEnabled(false);
            _lockService = new BaseLockService<TestLockTags>();
        }

        [Test]
        public void Lock_CallbackSubscribesMatchingLockable_LocksItOnce()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            var late = new RecordingLockable(TestLockTags.Tag1);
            first.OnLocking = () => _lockService.Subscribe(late);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            late.Calls.Should().Equal(true);
            first.Calls.Should().Equal(true);
            second.Calls.Should().Equal(true);

            handle.Dispose();

            late.Calls.Should().Equal(true, false);
            first.Calls.Should().Equal(true, false);
            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Lock_CallbackUnsubscribesPendingSubscriber_SkipsIt()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            first.OnLocking = () => _lockService.Unsubscribe(second);
            second.OnLocking = () => _lockService.Unsubscribe(first);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);
            handle.Dispose();

            // Assert
            var notified = first.Calls.Count > 0 ? first : second;
            var skipped = notified == first ? second : first;
            notified.Calls.Should().Equal(true, false);
            skipped.Calls.Should().BeEmpty();
        }

        [Test]
        public void Lock_CallbackUnsubscribesItself_GetsOneUnlock()
        {
            // Arrange
            var lockable = Subscribe(TestLockTags.Tag1);
            lockable.OnLocking = () => _lockService.Unsubscribe(lockable);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);
            handle.Dispose();

            // Assert
            lockable.Calls.Should().Equal(true, false);
            _lockService.IsLocked(lockable).Should().BeFalse();
        }

        [Test]
        public void Lock_CallbackUnsubscribesAndResubscribesOther_NotifiesNewSubscriptionOnce()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            var newSubscription = default(LockSubscription<TestLockTags>);
            first.OnLocking = () =>
            {
                _lockService.Unsubscribe(second);
                newSubscription = _lockService.Subscribe(second);
            };

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            second.Calls.Should().Equal(true);
            newSubscription.IsActive.Should().BeTrue();
            _lockService.IsLocked(second).Should().BeTrue();

            handle.Dispose();

            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Lock_CallbackAcquiresAnotherLock_AppliesBoth()
        {
            // Arrange
            var tag1 = Subscribe(TestLockTags.Tag1);
            var tag2 = Subscribe(TestLockTags.Tag2);
            var innerHandle = default(LockHandle<TestLockTags>);
            tag1.OnLocking = () => innerHandle = _lockService.Lock(TestLockTags.Tag2);

            // Act
            var outerHandle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            _lockService.IsLocked(tag1).Should().BeTrue();
            _lockService.IsLocked(tag2).Should().BeTrue();
            tag2.Calls.Should().Equal(true);

            outerHandle.Dispose();
            innerHandle.Dispose();

            tag1.Calls.Should().Equal(true, false);
            tag2.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Dispose_CallbackDisposesSameHandleAgain_UnlocksEachOnce()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            first.OnUnlocking = () => handle.Dispose();

            // Act
            handle.Dispose();

            // Assert
            first.Calls.Should().Equal(true, false);
            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Dispose_CallbackDisposesOtherHandle_UnlocksBoth()
        {
            // Arrange
            var tag1 = Subscribe(TestLockTags.Tag1);
            var tag2 = Subscribe(TestLockTags.Tag2);
            var tag1Handle = _lockService.Lock(TestLockTags.Tag1);
            var tag2Handle = _lockService.Lock(TestLockTags.Tag2);
            tag1.OnUnlocking = () => tag2Handle.Dispose();

            // Act
            tag1Handle.Dispose();

            // Assert
            tag1.Calls.Should().Equal(true, false);
            tag2.Calls.Should().Equal(true, false);
            tag2Handle.IsActive.Should().BeFalse();
        }

        [Test]
        public void Dispose_CallbackRelocksSameTag_KeepsOtherSubscriberLockedWithoutChurn()
        {
            // Arrange
            var relocker = Subscribe(TestLockTags.Tag1);
            var other = Subscribe(TestLockTags.Tag1);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            var relock = default(LockHandle<TestLockTags>);
            relocker.OnUnlocking = () => relock = _lockService.Lock(TestLockTags.Tag1);

            // Act
            handle.Dispose();

            // Assert
            relocker.Calls.Should().Equal(true, false, true);
            other.Calls.Should().Equal(true);
            _lockService.IsLocked(relocker).Should().BeTrue();
            _lockService.IsLocked(other).Should().BeTrue();

            relocker.OnUnlocking = null;
            relock.Dispose();
        }

        [Test]
        public void Lock_CallbackDisposesLockBeingApplied_LeavesNoSubscriberLocked()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            var third = Subscribe(TestLockTags.Tag1);
            first.OnLocking = () => _lockService.UnlockAll();

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            handle.IsActive.Should().BeFalse();

            first.Calls.Should().Equal(true, false);
            second.Calls.Should().BeEmpty();
            third.Calls.Should().BeEmpty();
            _lockService.IsLocked(first).Should().BeFalse();
            _lockService.IsLocked(second).Should().BeFalse();
            _lockService.IsLocked(third).Should().BeFalse();
        }

        [Test]
        public void UnlockAll_CallbackAcquiresNewLock_NewLockStaysActive()
        {
            // Arrange
            var tag1 = Subscribe(TestLockTags.Tag1);
            var tag2 = Subscribe(TestLockTags.Tag2);
            _lockService.Lock(TestLockTags.Tag1);
            var newHandle = default(LockHandle<TestLockTags>);
            tag1.OnUnlocking = () => newHandle = _lockService.Lock(TestLockTags.Tag2);

            // Act
            _lockService.UnlockAll();

            // Assert
            newHandle.IsActive.Should().BeTrue();
            _lockService.IsLocked(tag2).Should().BeTrue();
            tag2.Calls.Should().Equal(true);

            newHandle.Dispose();
        }

        [Test]
        public void UnlockAll_CallbackCallsUnlockAll_ReleasesEveryLockOnce()
        {
            // Arrange
            var tag1 = Subscribe(TestLockTags.Tag1);
            var tag2 = Subscribe(TestLockTags.Tag2);
            var tag3 = Subscribe(TestLockTags.Tag3);
            var tag1Handle = _lockService.Lock(TestLockTags.Tag1);
            var tag2Handle = _lockService.Lock(TestLockTags.Tag2);
            var tag3Handle = _lockService.Lock(TestLockTags.Tag3);
            tag1.OnUnlocking = () => _lockService.UnlockAll();

            // Act
            _lockService.UnlockAll();

            // Assert
            tag1Handle.IsActive.Should().BeFalse();
            tag2Handle.IsActive.Should().BeFalse();
            tag3Handle.IsActive.Should().BeFalse();
            tag1.Calls.Should().Equal(true, false);
            tag2.Calls.Should().Equal(true, false);
            tag3.Calls.Should().Equal(true, false);
        }

        [Test]
        public void UnlockAll_WhenOneReleaseThrows_ReleasesTheRestAndRethrows()
        {
            // Arrange
            var thrower = Subscribe(TestLockTags.Tag1);
            var other = Subscribe(TestLockTags.Tag2);
            var throwerHandle = _lockService.Lock(TestLockTags.Tag1);
            var otherHandle = _lockService.Lock(TestLockTags.Tag2);
            thrower.OnUnlocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => _lockService.UnlockAll();

            // Assert
            act.Should().Throw<InvalidOperationException>();
            throwerHandle.IsActive.Should().BeFalse();
            otherHandle.IsActive.Should().BeFalse();
            other.Calls.Should().Equal(true, false);
            _lockService.IsLocked(other).Should().BeFalse();
        }

        [Test]
        public void Subscribe_CallbackSubscribesAnother_LocksBothOnce()
        {
            // Arrange
            var handle = _lockService.Lock(TestLockTags.Tag1);
            var first = new RecordingLockable(TestLockTags.Tag1);
            var second = new RecordingLockable(TestLockTags.Tag1);
            first.OnLocking = () => _lockService.Subscribe(second);

            // Act
            _lockService.Subscribe(first);

            // Assert
            first.Calls.Should().Equal(true);
            second.Calls.Should().Equal(true);

            handle.Dispose();

            first.Calls.Should().Equal(true, false);
            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Unsubscribe_CallbackUnsubscribesAnother_UnlocksBothOnce()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            first.OnUnlocking = () => _lockService.Unsubscribe(second);

            // Act
            _lockService.Unsubscribe(first);

            // Assert
            first.Calls.Should().Equal(true, false);
            second.Calls.Should().Equal(true, false);
            _lockService.IsLocked(second).Should().BeFalse();

            handle.Dispose();

            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Lock_CallbackThrows_ServiceStaysUsable()
        {
            // Arrange
            var thrower = Subscribe(TestLockTags.Tag1);
            var tag2 = Subscribe(TestLockTags.Tag2);
            thrower.OnLocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => _lockService.Lock(TestLockTags.Tag1);

            // Assert
            act.Should().Throw<InvalidOperationException>();

            var handle = _lockService.Lock(TestLockTags.Tag2);
            tag2.Calls.Should().Equal(true);
            handle.Dispose();
            tag2.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Lock_WhenHandleLockingThrows_RethrowsAndLeavesNoActiveLock()
        {
            // Arrange
            var thrower = Subscribe(TestLockTags.Tag1);
            thrower.OnLocking = () => throw new InvalidOperationException("callback failure");
            var debugInfo = new List<LockDebugInfo>();

            // Act
            Action act = () => _lockService.Lock(TestLockTags.Tag1);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("callback failure");
            thrower.Calls.Should().Equal(true, false);
            _lockService.IsLocked(thrower).Should().BeFalse();
            _lockService.PopulateDebugInfo(debugInfo);
            debugInfo.Should().BeEmpty();
            var late = Subscribe(TestLockTags.Tag1);
            _lockService.IsLocked(late).Should().BeFalse();
            late.Calls.Should().BeEmpty();
        }

        [Test]
        public void Lock_WhenOneLockableThrows_OtherLockablesAreStillNotified()
        {
            // Arrange
            var thrower = Subscribe(TestLockTags.Tag1);
            var other = Subscribe(TestLockTags.Tag1);
            thrower.OnLocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => _lockService.Lock(TestLockTags.Tag1);

            // Assert
            act.Should().Throw<InvalidOperationException>();
            other.Calls.Should().Equal(true, false);
            _lockService.IsLocked(other).Should().BeFalse();
        }

        [Test]
        public void Lock_WhenTwoLockablesThrow_ThrowsAggregateException()
        {
            // Arrange
            var first = Subscribe(TestLockTags.Tag1);
            var second = Subscribe(TestLockTags.Tag1);
            first.OnLocking = () => throw new InvalidOperationException("first");
            second.OnLocking = () => throw new ArgumentException("second");

            // Act
            Action act = () => _lockService.Lock(TestLockTags.Tag1);

            // Assert
            var thrown = act.Should().Throw<AggregateException>().Which;
            thrown.InnerExceptions.Should().HaveCount(2);
            thrown.InnerExceptions[0].Should().BeOfType<InvalidOperationException>();
            thrown.InnerExceptions[1].Should().BeOfType<ArgumentException>();
            first.Calls.Should().Equal(true, false);
            second.Calls.Should().Equal(true, false);
        }

        [Test]
        public void Dispose_WhenHandleUnlockingThrows_OtherLockablesAreStillUnlocked()
        {
            // Arrange
            var thrower = Subscribe(TestLockTags.Tag1);
            var other = Subscribe(TestLockTags.Tag1);
            var handle = _lockService.Lock(TestLockTags.Tag1);
            thrower.OnUnlocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => handle.Dispose();

            // Assert
            act.Should().Throw<InvalidOperationException>();
            handle.IsActive.Should().BeFalse();
            other.Calls.Should().Equal(true, false);
            _lockService.IsLocked(other).Should().BeFalse();
            _lockService.IsLocked(thrower).Should().BeFalse();
        }

        [Test]
        public void Subscribe_WhenHandleLockingThrows_RethrowsAndLeavesItUnsubscribed()
        {
            // Arrange
            var handle = _lockService.Lock(TestLockTags.Tag1);
            var thrower = new RecordingLockable(TestLockTags.Tag1);
            thrower.OnLocking = () => throw new InvalidOperationException("callback failure");

            // Act
            Action act = () => _lockService.Subscribe(thrower);

            // Assert
            act.Should().Throw<InvalidOperationException>();
            thrower.Calls.Should().Equal(true, false);
            _lockService.IsLocked(thrower).Should().BeFalse();

            handle.Dispose();
            thrower.OnLocking = null;
            var laterHandle = _lockService.Lock(TestLockTags.Tag1);
            thrower.Calls.Should().Equal(true, false);
            laterHandle.Dispose();
        }

        [Test]
        public void LockGate_CallbackDisposesItsOwnGate_OtherGateStaysPaired()
        {
            // Arrange
            var otherChanges = new List<bool>();
            var selfChanges = new List<bool>();
            LockGate<TestLockTags> self = null;
            self = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, isLocked =>
            {
                selfChanges.Add(isLocked);
                self.Dispose();
            });
            var other = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, otherChanges.Add);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            selfChanges.Should().Equal(true);
            self.IsLocked.Should().BeFalse();
            other.IsLocked.Should().BeTrue();

            handle.Dispose();

            selfChanges.Should().Equal(true);
            otherChanges.Should().Equal(true, false);
            other.Dispose();
        }

        [Test]
        public void LockGate_CallbackTurnsOffSwitchThatLockedIt_EndsUnlockedAndPaired()
        {
            // Arrange
            var lockSwitch = _lockService.CreateSwitch(TestLockTags.Tag1);
            var firstChanges = new List<bool>();
            var secondChanges = new List<bool>();
            var first = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, isLocked =>
            {
                firstChanges.Add(isLocked);
                lockSwitch.Set(false);
            });
            var second = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, secondChanges.Add);

            // Act
            lockSwitch.Set(true);

            // Assert
            lockSwitch.IsOn.Should().BeFalse();
            first.IsLocked.Should().BeFalse();
            second.IsLocked.Should().BeFalse();
            firstChanges.Should().Equal(true, false);
            secondChanges.Should().Equal(true, false);

            first.Dispose();
            second.Dispose();
        }

        [Test]
        public void LockGate_CallbackTurnsOnOtherSwitch_LocksItsGatesOnce()
        {
            // Arrange
            var tag2Switch = _lockService.CreateSwitch(TestLockTags.Tag2);
            var tag2Changes = new List<bool>();
            var tag1Gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag1, tag2Switch.Set);
            var tag2Gate = new LockGate<TestLockTags>(_lockService, TestLockTags.Tag2, tag2Changes.Add);

            // Act
            var handle = _lockService.Lock(TestLockTags.Tag1);

            // Assert
            tag2Switch.IsOn.Should().BeTrue();
            tag2Gate.IsLocked.Should().BeTrue();
            tag2Changes.Should().Equal(true);

            handle.Dispose();

            tag2Switch.IsOn.Should().BeFalse();
            tag2Changes.Should().Equal(true, false);

            tag1Gate.Dispose();
            tag2Gate.Dispose();
        }

        [Test]
        public void Lock_WithReentrantLockAndUnsubscribeInCallbacks_DoesNotAllocate()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>(lockCapacity: 8, lockableCapacity: 8);
            var locker = new CallbackLockable(TestLockTags.Tag1);
            var resubscriber = new CallbackLockable(TestLockTags.Tag1);
            var target = new CallbackLockable(TestLockTags.Tag1);
            var tag2 = new CallbackLockable(TestLockTags.Tag2);
            var innerHandle = default(LockHandle<TestLockTags>);
            locker.OnLocking = () => innerHandle = service.Lock(TestLockTags.Tag2);
            locker.OnUnlocking = () => innerHandle.Dispose();
            resubscriber.OnLocking = () => service.Unsubscribe(target);
            resubscriber.OnUnlocking = () => service.Subscribe(target);
            service.Subscribe(locker);
            service.Subscribe(resubscriber);
            service.Subscribe(target);
            service.Subscribe(tag2);
            TestDelegate act = () =>
            {
                var handle = service.Lock(TestLockTags.Tag1);
                handle.Dispose();
            };
            act();

            // Act & Assert
            Assert.That(act, Is.Not.AllocatingGCMemory());
            tag2.LockCalls.Should().Be(tag2.UnlockCalls);
            service.IsLocked(target).Should().BeFalse();
        }

        private RecordingLockable Subscribe(TestLockTags tags)
        {
            var lockable = new RecordingLockable(tags);
            _lockService.Subscribe(lockable);
            return lockable;
        }

        private sealed class RecordingLockable : ILockable<TestLockTags>
        {
            public TestLockTags LockTags { get; }
            public List<bool> Calls { get; } = new List<bool>();
            public Action OnLocking { get; set; }
            public Action OnUnlocking { get; set; }

            public RecordingLockable(TestLockTags tags)
            {
                LockTags = tags;
            }

            public void HandleLocking()
            {
                Calls.Add(true);
                OnLocking?.Invoke();
            }

            public void HandleUnlocking()
            {
                Calls.Add(false);
                OnUnlocking?.Invoke();
            }
        }

        private sealed class CallbackLockable : ILockable<TestLockTags>
        {
            public TestLockTags LockTags { get; }
            public int LockCalls { get; private set; }
            public int UnlockCalls { get; private set; }
            public Action OnLocking { get; set; }
            public Action OnUnlocking { get; set; }

            public CallbackLockable(TestLockTags tags)
            {
                LockTags = tags;
            }

            public void HandleLocking()
            {
                LockCalls++;
                OnLocking?.Invoke();
            }

            public void HandleUnlocking()
            {
                UnlockCalls++;
                OnUnlocking?.Invoke();
            }
        }
    }
}
