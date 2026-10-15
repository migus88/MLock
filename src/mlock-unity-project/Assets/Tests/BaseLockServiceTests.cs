using System;
using FluentAssertions;
using Migs.MLock.Interfaces;
using Migs.MLock.Tests.Integration;
using NSubstitute;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class BaseLockServiceTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private ILockable<TestLockTags> _mockLockable;
        
        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>();
            _mockLockable = Substitute.For<ILockable<TestLockTags>>();
            _mockLockable.LockTags.Returns(TestLockTags.Tag1);
        }
        
        [Test]
        public void Subscribe_WithValidLockable_AddsToInternalCollection()
        {
            // Act
            _lockService.Subscribe(_mockLockable);
            
            // Assert
            _lockService.IsLocked(_mockLockable).Should().BeFalse(); // Initially not locked
        }
        
        [Test]
        public void Subscribe_WithNullLockable_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _lockService.Subscribe(null);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Lockable cannot be null*");
        }
        
        [Test]
        public void Subscribe_WhenAlreadySubscribed_ThrowsArgumentException()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            
            // Act & Assert
            Action act = () => _lockService.Subscribe(_mockLockable);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Lockable * is already subscribed*");
        }
        
        [Test]
        public void Unsubscribe_WithSubscribedLockable_RemovesFromInternalCollection()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            
            // Create a lock that should apply to this lockable
            var lockObj = _lockService.Lock(TestLockTags.Tag1);
            _lockService.IsLocked(_mockLockable).Should().BeTrue();
            
            // Act
            _lockService.Unsubscribe(_mockLockable);
            
            // Assert
            _mockLockable.Received(1).HandleUnlocking();
            
            // Try to check if it's still locked, should return false since it's unsubscribed
            _lockService.IsLocked(_mockLockable).Should().BeFalse();
            
            // Clean up
            lockObj.Dispose();
        }
        
        [Test]
        public void Unsubscribe_WithNullLockable_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _lockService.Unsubscribe(null);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Lockable cannot be null*");
        }
        
        [Test]
        public void Unsubscribe_WithNonSubscribedLockable_DoesNothing()
        {
            // Act
            _lockService.Unsubscribe(_mockLockable);
            
            // Assert - no exception means success
        }
        
        [Test]
        public void Lock_WithValidTags_CreatesAndAddsLock()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            
            // Act
            var lockObj = _lockService.Lock(TestLockTags.Tag1);
            
            // Assert
            lockObj.IsActive.Should().BeTrue();
            _lockService.IsLocked(_mockLockable).Should().BeTrue();
            
            // Clean up
            lockObj.Dispose();
        }
        
        [Test]
        public void LockAllExcept_WithValidTags_CreatesAndAddsLock()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            
            // Act
            var lockObj = _lockService.LockAllExcept(TestLockTags.Tag2);
            
            // Assert
            lockObj.IsActive.Should().BeTrue();
            _lockService.IsLocked(_mockLockable).Should().BeTrue(); // Has Tag1 not Tag2, so should be locked
            
            // Clean up
            lockObj.Dispose();
        }
        
        [Test]
        public void LockAll_CreatesAndAddsLock()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            
            // Act
            var lockObj = _lockService.LockAll();
            
            // Assert
            lockObj.IsActive.Should().BeTrue();
            _lockService.IsLocked(_mockLockable).Should().BeTrue();
            
            // Clean up
            lockObj.Dispose();
        }
        
        [Test]
        public void IsLocked_WithNullLockable_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _lockService.IsLocked(null);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Lockable cannot be null*");
        }
        
        [Test]
        public void Lock_LocksMatchingLockable()
        {
            // Arrange
            var mockLockable1 = Substitute.For<ILockable<TestLockTags>>();
            var mockLockable2 = Substitute.For<ILockable<TestLockTags>>();
            
            mockLockable1.LockTags.Returns(TestLockTags.Tag1);
            mockLockable2.LockTags.Returns(TestLockTags.Tag2);
            
            _lockService.Subscribe(mockLockable1);
            _lockService.Subscribe(mockLockable2);
            
            // Act
            var lockObj = _lockService.Lock(TestLockTags.Tag1);
            
            // Assert
            _lockService.IsLocked(mockLockable1).Should().BeTrue();
            _lockService.IsLocked(mockLockable2).Should().BeFalse();
            
            mockLockable1.Received(1).HandleLocking();
            mockLockable2.DidNotReceive().HandleLocking();
            
            // Clean up
            lockObj.Dispose();
        }
        
        [Test]
        public void Dispose_RemovesLock()
        {
            // Arrange
            _lockService.Subscribe(_mockLockable);
            var lockObj = _lockService.Lock(TestLockTags.Tag1);
            _lockService.IsLocked(_mockLockable).Should().BeTrue();
            _mockLockable.Received(1).HandleLocking();
            
            // Act
            lockObj.Dispose();
            
            // Assert
            _lockService.IsLocked(_mockLockable).Should().BeFalse();
            _mockLockable.Received(1).HandleUnlocking();
        }
        
        [Test]
        public void MultipleLocks_AreCorrectlyTracked()
        {
            // Arrange
            var mockLockable = Substitute.For<ILockable<TestLockTags>>();
            mockLockable.LockTags.Returns(TestLockTags.Tag1);
            _lockService.Subscribe(mockLockable);
            
            // Act - create two locks that apply to the lockable
            var lock1 = _lockService.Lock(TestLockTags.Tag1);
            var lock2 = _lockService.Lock(TestLockTags.Tag1);
            
            // Assert - should be locked because of both locks
            _lockService.IsLocked(mockLockable).Should().BeTrue();
            mockLockable.Received(1).HandleLocking(); // Only called once even with multiple locks
            
            // Clean up the first lock only
            lock1.Dispose();
            
            // Should still be locked due to the second lock
            _lockService.IsLocked(mockLockable).Should().BeTrue();
            mockLockable.DidNotReceive().HandleUnlocking(); // Not unlocked yet
            
            // Remove the final lock
            lock2.Dispose();
            
            // Now it should be unlocked
            _lockService.IsLocked(mockLockable).Should().BeFalse();
            mockLockable.Received(1).HandleUnlocking(); // Called once after all locks are removed
        }
    }
} 