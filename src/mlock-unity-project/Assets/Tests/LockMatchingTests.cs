using System;
using FluentAssertions;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class LockMatchingTests
    {
        [Test]
        public void Lock_None_LocksNothing()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>();
            var tagged = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            var untagged = new CountingLockable<TestLockTags>(TestLockTags.None);
            service.Subscribe(tagged);
            service.Subscribe(untagged);

            // Act
            var handle = service.Lock(TestLockTags.None);

            // Assert
            handle.IsActive.Should().BeTrue();
            service.IsLocked(tagged).Should().BeFalse();
            service.IsLocked(untagged).Should().BeFalse();
            tagged.LockCalls.Should().Be(0);
            untagged.LockCalls.Should().Be(0);
        }

        [Test]
        public void LockAll_LocksLockableWithNoTags()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>();
            var untagged = new CountingLockable<TestLockTags>(TestLockTags.None);
            service.Subscribe(untagged);

            // Act
            var handle = service.LockAll();

            // Assert
            service.IsLocked(untagged).Should().BeTrue();

            handle.Dispose();
            service.IsLocked(untagged).Should().BeFalse();
        }

        [Test]
        public void LockAllExcept_None_LocksEverything()
        {
            // Arrange
            var service = new BaseLockService<TestLockTags>();
            var tagged = new CountingLockable<TestLockTags>(TestLockTags.Tag1);
            var untagged = new CountingLockable<TestLockTags>(TestLockTags.None);
            service.Subscribe(tagged);
            service.Subscribe(untagged);

            // Act
            service.LockAllExcept(TestLockTags.None);

            // Assert
            service.IsLocked(tagged).Should().BeTrue();
            service.IsLocked(untagged).Should().BeTrue();
        }

        [Test]
        public void Lock_SignedEnumAllValue_LocksTaggedLockableWithoutThrowing()
        {
            // Arrange
            var service = new BaseLockService<SignedTestTags>();
            var lockable = new CountingLockable<SignedTestTags>(SignedTestTags.Tag1);
            service.Subscribe(lockable);

            // Act
            Action act = () => service.Lock(SignedTestTags.All);

            // Assert
            act.Should().NotThrow();
            service.IsLocked(lockable).Should().BeTrue();
        }

        [Test]
        public void Lock_NegativeEnumMember_MatchesItselfOnly()
        {
            // Arrange
            var service = new BaseLockService<SignedTestTags>();
            var negative = new CountingLockable<SignedTestTags>(SignedTestTags.Negative);
            var positive = new CountingLockable<SignedTestTags>(SignedTestTags.Tag1);
            service.Subscribe(negative);
            service.Subscribe(positive);

            // Act
            service.Lock(SignedTestTags.Negative);

            // Assert
            service.IsLocked(negative).Should().BeTrue();
            service.IsLocked(positive).Should().BeFalse();
        }

        [Test]
        public void Lock_UlongEnumHighBit_LocksOnlyLockableWithThatBit()
        {
            // Arrange
            var service = new BaseLockService<WideTestTags>();
            var high = new CountingLockable<WideTestTags>(WideTestTags.Bit40);
            var low = new CountingLockable<WideTestTags>(WideTestTags.Bit8);
            service.Subscribe(high);
            service.Subscribe(low);

            // Act
            service.Lock(WideTestTags.Bit40);

            // Assert
            service.IsLocked(high).Should().BeTrue();
            service.IsLocked(low).Should().BeFalse();
        }

        [Test]
        public void Lock_ByteEnum_LocksOnlyMatchingLockable()
        {
            // Arrange
            var service = new BaseLockService<ByteTestTags>();
            var first = new CountingLockable<ByteTestTags>(ByteTestTags.Tag1);
            var second = new CountingLockable<ByteTestTags>(ByteTestTags.Tag2);
            service.Subscribe(first);
            service.Subscribe(second);

            // Act
            service.Lock(ByteTestTags.Tag2);

            // Assert
            service.IsLocked(first).Should().BeFalse();
            service.IsLocked(second).Should().BeTrue();
        }
    }
}
