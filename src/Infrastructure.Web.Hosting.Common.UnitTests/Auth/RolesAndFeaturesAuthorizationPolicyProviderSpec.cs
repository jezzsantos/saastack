using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Auth;

[Trait("Category", "Unit")]
public class RolesAndFeaturesAuthorizationPolicyProviderSpec
{
    private readonly RolesAndFeaturesAuthorizationPolicyProvider _provider;

    public RolesAndFeaturesAuthorizationPolicyProviderSpec()
    {
        var options = new Mock<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>();
        options.Setup(opt => opt.Value).Returns(new Microsoft.AspNetCore.Authorization.AuthorizationOptions());
        _provider = new RolesAndFeaturesAuthorizationPolicyProvider(options.Object);
    }

    [Fact]
    public async Task WhenGetPolicyAsyncAndNotCachedAndUnknown_ThenBuildsPolicy()
    {
        var policyName =
            $"POLICY:{{|Features|:{{|Platform|:[|{PlatformFeatures.Basic.Name}|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}";

        var result = await _provider.GetPolicyAsync(policyName);

        result!.Requirements.Count.Should().Be(2);
        result.Requirements[1].Should().BeOfType<RolesAndFeaturesRequirement>();
        result.Requirements[1].As<RolesAndFeaturesRequirement>().Roles.All.Should()
            .ContainSingle(PlatformRoles.Standard.Name);
        result.Requirements[1].As<RolesAndFeaturesRequirement>().Features.All.Should()
            .ContainSingle(PlatformFeatures.Basic.Name);

#if TESTINGONLY
        _provider.IsCached(policyName).Should().BeTrue();
#endif
    }

    [Fact]
    public async Task WhenGetPolicyAsyncAndCached_ThenReturnsCachedPolicy()
    {
        var builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build();
        var policyName =
            $"POLICY:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{{{PlatformRoles.Standard.Name}}}|]}}}}";
#if TESTINGONLY
        _provider.CachePolicy(policyName, builder);
#endif

        var result = await _provider.GetPolicyAsync(policyName);

        result.Should().Be(builder);

#if TESTINGONLY
        _provider.IsCached(policyName).Should().BeTrue();
#endif
    }
}