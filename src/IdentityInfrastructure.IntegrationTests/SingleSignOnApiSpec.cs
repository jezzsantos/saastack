using System.Net;
using ApiHost1;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class SingleSignOnApiSpec : WebApiSpec<Program>
{
    public SingleSignOnApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenAuthenticateAndUnknownProvider_ThenReturnsError()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Provider = "anunknownprovider",
            AuthCode = "1234567890"
        });

        result.Content.Error.Status.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndNoUsername_ThenReturnsError()
    {
#if TESTINGONLY
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = null,
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = "1234567890"
        });

        result.Content.Error.Status.Should().Be((int)HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenAuthenticateAndWrongAuthCode_ThenReturnsError()
    {
#if TESTINGONLY
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = "awrongcode"
        });

        result.Content.Error.Status.Should().Be((int)HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenAuthenticateAndUserNotExists_ThenRegistersAndReturnsUser()
    {
#if TESTINGONLY
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = "1234567890"
        });

        result.Content.Value.Tokens.UserId.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
#endif
    }

    [Fact]
    public async Task WhenAuthenticateAndUserExists_ThenReturnsTokens()
    {
#if TESTINGONLY
        var person = await Api.PostAsync(new RegisterPersonPasswordRequest
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
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = "1234567890"
        });

        result.Content.Value.Tokens.UserId.Should().Be(person.Content.Value.Credential.User.Id);
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
#endif
    }

    [Fact]
    public async Task WhenCallingSecureApiAfterAuthenticate_ThenReturnsResponse()
    {
#if TESTINGONLY
        var authenticate = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = "1234567890"
        });

        var accessToken = authenticate.Content.Value.Tokens.AccessToken.Value;

        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetJWTBearerToken(accessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(authenticate.Content.Value.Tokens.UserId);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
}