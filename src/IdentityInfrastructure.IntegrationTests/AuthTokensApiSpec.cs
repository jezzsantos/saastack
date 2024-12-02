using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class AuthTokensApiSpec : WebApiSpec<Program>
{
    public AuthTokensApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
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

        await PropagateDomainEventsAsync();
        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonPasswordRequest
        {
            Token = token!
        });

        await PropagateDomainEventsAsync();
        var oldTokens = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        oldTokens.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        oldTokens.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        oldTokens.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        oldTokens.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));

        await Task.Delay(TimeSpan
            .FromSeconds(1)); //HACK: to ensure that the new token is not the same (in time) as the old token

        var oldAccessToken = oldTokens.Content.Value.Tokens.AccessToken.Value;
        var oldRefreshToken = oldTokens.Content.Value.Tokens.RefreshToken.Value;
        var newTokens = await Api.PostAsync(new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        });

        newTokens.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull().And.NotBe(oldAccessToken);
        newTokens.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        newTokens.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull().And.NotBe(oldRefreshToken);
        newTokens.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
    }

    [Fact]
    public async Task WhenRevokeRefreshToken_ThenRevokes()
    {
        var user = await LoginUserAsync();

        var oldRefreshToken = user.RefreshToken;
        var revoked = await Api.DeleteAsync(new RevokeRefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        });

        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshed = await Api.PostAsync(new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        });

        refreshed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}