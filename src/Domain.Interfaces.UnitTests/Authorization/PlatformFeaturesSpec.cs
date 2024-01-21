using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class PlatformFeaturesSpec
{
#if TESTINGONLY
    [Fact]
    public void WhenAssignableFeatureSets_ThenReturnsSome()
    {
        var result = PlatformFeatures.PlatformAssignableFeatures;

        result.Count.Should().BeGreaterThan(0);
        result.Should().Contain(PlatformFeatures.TestingOnly);
    }
#endif
}