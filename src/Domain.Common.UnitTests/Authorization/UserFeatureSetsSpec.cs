using Domain.Common.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class UserFeatureSetsSpec
{
#if TESTINGONLY
    [Fact]
    public void WhenAssignableFeatureSets_ThenReturnsSome()
    {
        var result = UserFeatureSets.AssignableFeatureSets;

        result.Count.Should().BeGreaterThan(0);
        result.Should().Contain(UserFeatureSets.TestingOnlyFeatures);
    }
#endif
}