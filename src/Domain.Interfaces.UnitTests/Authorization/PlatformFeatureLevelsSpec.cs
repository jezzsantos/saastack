using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class PlatformFeatureLevelsSpec
{
#if TESTINGONLY
    [Fact]
    public void WhenAssignableFeatureSets_ThenReturnsSome()
    {
        var result = PlatformFeatureLevels.PlatformAssignableFeatureLevels;

        result.Count.Should().BeGreaterThan(0);
        result.Should().Contain(PlatformFeatureLevels.TestingOnlyLevel);
    }
#endif
}