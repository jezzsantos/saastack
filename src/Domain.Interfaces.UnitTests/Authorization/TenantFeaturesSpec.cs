using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class TenantFeaturesSpec
{
#if TESTINGONLY
    [Fact]
    public void WhenAssignableFeatureSets_ThenReturnsSome()
    {
        var result = TenantFeatures.TenantAssignableFeatures;

        result.Count.Should().BeGreaterThan(0);
        result.Should().Contain(TenantFeatures.TestingOnly);
    }
#endif
}