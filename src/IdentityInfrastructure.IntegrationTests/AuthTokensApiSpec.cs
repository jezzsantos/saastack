using ApiHost1;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class AuthTokensSpec : WebApiSpec<Program>
{
    public AuthTokensSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
    }

    [Fact]
    public async Task WhenRefreshToken_ThenReturnsNewTokens()
    {
        await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        var token = NotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonPasswordRequest
        {
            Token = token!
        });

        var oldTokens = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        oldTokens.Content.Value.AccessToken.Should().NotBeNull();
        oldTokens.Content.Value.RefreshToken.Should().NotBeNull();
        oldTokens.Content.Value.ExpiresOnUtc.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.DefaultAccessTokenExpiry));

        await Task.Delay(TimeSpan
            .FromSeconds(1)); //HACK: to ensure that the new token is not the same (in time) as the old token

        var oldAccessToken = oldTokens.Content.Value.AccessToken;
        var oldRefreshToken = oldTokens.Content.Value.RefreshToken;
        var newTokens = await Api.PostAsync(new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken!
        });

        newTokens.Content.Value.AccessToken.Should().NotBeNull().And.NotBe(oldAccessToken);
        newTokens.Content.Value.RefreshToken.Should().NotBeNull().And.NotBe(oldRefreshToken);
        newTokens.Content.Value.ExpiresOnUtc.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.DefaultAccessTokenExpiry));
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}