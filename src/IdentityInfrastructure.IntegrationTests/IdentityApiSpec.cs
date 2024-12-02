using ApiHost1;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class IdentityApiSpec : WebApiSpec<Program>
{
    public IdentityApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenGetIdentity_ThenReturnsIdentity()
    {
        var login = await LoginUserAsync();

        var result = await Api.GetAsync(new GetIdentityForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Identity.Id.Should().NotBeEmpty();
        result.Content.Value.Identity.IsMfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task WhenGetIdentityAndMfaEnabled_ThenReturnsIdentity()
    {
        var login = await LoginUserAsync();

        await Api.PutAsync(new ChangePasswordMfaForCallerRequest
        {
            IsEnabled = true
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.GetAsync(new GetIdentityForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Identity.IsMfaEnabled.Should().BeTrue();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}