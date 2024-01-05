using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class MemberFeatureLevelsSpec
{
#if TESTINGONLY
    [Fact]
    public void WhenAssignableFeatureSets_ThenReturnsSome()
    {
        var result = MemberFeatureLevels.MemberAssignableFeatureLevels;

        result.Count.Should().BeGreaterThan(0);
        result.Should().Contain(MemberFeatureLevels.TestingOnlyFeatures);
    }
#endif
}