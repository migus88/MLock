using System.Collections.Generic;
using FluentAssertions;
using Migs.MLock.Debugging;
using Migs.MLock.Tests.Integration;
using NUnit.Framework;

namespace Migs.MLock.Tests
{
    public class LockDebugInfoTests
    {
        private BaseLockService<TestLockTags> _lockService;
        private List<LockDebugInfo> _debugInfo;

        [SetUp]
        public void Setup()
        {
            _lockService = new BaseLockService<TestLockTags>();
            _debugInfo = new List<LockDebugInfo>();
        }

        [TearDown]
        public void TearDown()
        {
            DebugDataHandler.SetEnabled(false);
        }

        [Test]
        public void Lock_WhenDebugDisabled_CapturesNoOrigin()
        {
            // Arrange
            DebugDataHandler.SetEnabled(false);

            // Act
            _lockService.Lock(TestLockTags.Tag1);

            // Assert
            _lockService.PopulateDebugInfo(_debugInfo);
            _debugInfo.Should().ContainSingle();
            _debugInfo[0].OriginFrames.Should().BeNull();
        }

        [Test]
        public void Lock_WhenDebugEnabled_CapturesOrigin()
        {
            // Arrange
            DebugDataHandler.SetEnabled(true);

            // Act
            _lockService.Lock(TestLockTags.Tag1);

            // Assert
            _lockService.PopulateDebugInfo(_debugInfo);
            _debugInfo.Should().ContainSingle();
            _debugInfo[0].OriginFrames.Should().NotBeNullOrEmpty();
            _debugInfo[0].OriginFrames[0].Display.Should().Contain(nameof(Lock_WhenDebugEnabled_CapturesOrigin));
        }

        [Test]
        public void PopulateDebugInfo_ListsAffectedLockablesAndMode()
        {
            // Arrange
            _lockService.Subscribe(new CountingLockable<TestLockTags>(TestLockTags.Tag1, "First"));
            _lockService.Subscribe(new CountingLockable<TestLockTags>(TestLockTags.Tag2, "Second"));
            _lockService.Subscribe(new CountingLockable<TestLockTags>(TestLockTags.Tag3, "Third"));
            _lockService.LockAllExcept(TestLockTags.Tag2);

            // Act
            _lockService.PopulateDebugInfo(_debugInfo);

            // Assert
            _debugInfo.Should().ContainSingle();
            var info = _debugInfo[0];
            info.Mode.Should().Be("Exclude");
            info.ExcludeTags.Should().Be(nameof(TestLockTags.Tag2));
            info.IncludeTags.Should().BeNull();
            info.LockType.Should().Be(nameof(TestLockTags));
            info.AffectedLockables.Should().BeEquivalentTo("First", "Third");
        }
    }
}
