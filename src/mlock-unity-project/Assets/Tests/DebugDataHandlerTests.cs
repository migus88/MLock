using System;
using FluentAssertions;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using Migs.MLock.Tests.Integration;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Migs.MLock.Tests
{
    public class DebugDataHandlerTests
    {
        [SetUp]
        public void Setup()
        {
            DebugDataHandler.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            DebugDataHandler.SetEnabled(false);
            DebugDataHandler.Reset();
        }

        [Test]
        public void UnlockAll_WithMultipleActiveLocks_ReleasesEveryLock()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>();
            var lockable1 = CreateLockable(TestLockTags.Tag1);
            var lockable2 = CreateLockable(TestLockTags.Tag2);
            service.Subscribe(lockable1);
            service.Subscribe(lockable2);
            var lock1 = service.Lock(TestLockTags.Tag1);
            var lock2 = service.Lock(TestLockTags.Tag1);
            var lock3 = service.Lock(TestLockTags.Tag2);

            // Act
            Action act = () => ((IDebuggableLockService)service).UnlockAll();

            // Assert
            act.Should().NotThrow();
            service.IsLocked(lockable1).Should().BeFalse();
            service.IsLocked(lockable2).Should().BeFalse();
            lockable1.Received(1).HandleUnlocking();
            lockable2.Received(1).HandleUnlocking();
        }

        [Test]
        public void HandlerUnlockAll_WhenUnlockingUnregistersAService_ReleasesLocksOfEveryService()
        {
            // Arrange
            var service1 = new BaseLockService<TestLockTags>();
            var service2 = new BaseLockService<TestLockTags>();
            var debuggable1 = (IDebuggableLockService)service1;
            var debuggable2 = (IDebuggableLockService)service2;
            debuggable1.WithDebug<TestLockTags>();
            debuggable2.WithDebug<TestLockTags>();
            var lockable1 = CreateLockable(TestLockTags.Tag1);
            var lockable2 = CreateLockable(TestLockTags.Tag1);
            lockable1.When(l => l.HandleUnlocking()).Do(_ => debuggable1.WithoutDebug<TestLockTags>());
            service1.Subscribe(lockable1);
            service2.Subscribe(lockable2);
            var lock1 = service1.Lock(TestLockTags.Tag1);
            var lock2 = service2.Lock(TestLockTags.Tag1);

            // Act
            Action act = () => DebugDataHandler.UnlockAll();

            // Assert
            act.Should().NotThrow();
            service1.IsLocked(lockable1).Should().BeFalse();
            service2.IsLocked(lockable2).Should().BeFalse();
        }

        [Test]
        public void RegisterAndUnregister_LogNothing()
        {
            // Arrange
            var debuggable = (IDebuggableLockService)new BaseLockService<TestLockTags>();

            // Act
            debuggable.WithDebug<TestLockTags>();
            debuggable.WithoutDebug<TestLockTags>();

            // Assert
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WithoutDebug_OnLockService_UnregistersService()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>().WithDebug();

            // Act
            service.WithoutDebug();

            // Assert
            DebugDataHandler.LockServices.Should().NotContain((IDebuggableLockService)service);
        }

        [Test]
        public void Reset_WithRegisteredServices_ClearsRegistry()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>();
            ((IDebuggableLockService)service).WithDebug<TestLockTags>();
            ((IDebuggableLockService)new BaseLockService<TestLockTags>()).WithDebug<TestLockTags>();
            var activeLock = service.Lock(TestLockTags.Tag1);
            DebugDataHandler.SetEnabled(true);
            DebugDataHandler.UpdateData();
            DebugDataHandler.LockServices.Should().HaveCount(2);
            DebugDataHandler.ActiveLocks.Should().HaveCount(1);

            // Act
            DebugDataHandler.Reset();

            // Assert
            DebugDataHandler.LockServices.Should().BeEmpty();
            DebugDataHandler.ActiveLocks.Should().BeEmpty();
        }

        private static ILockable<TestLockTags> CreateLockable(TestLockTags tags)
        {
            var lockable = Substitute.For<ILockable<TestLockTags>>();
            lockable.LockTags.Returns(tags);
            return lockable;
        }
    }
}
