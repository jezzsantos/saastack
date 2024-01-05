using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Hosting.Common.Auth;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class PasswordCredentialsApiSpec : WebApiSpec<Program>
{
    private readonly StubNotificationsService _notificationsService;

    public PasswordCredentialsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
        _notificationsService = setup.GetRequiredService<INotificationsService>().As<StubNotificationsService>();
    }

    [Fact]
    public async Task WhenRegisterPerson_ThenRegisters()
    {
        var result = await Api.PostAsync(new RegisterPersonRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Credential!.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Access.Should().Be(EndUserAccess.Enabled);
        result.Content.Value.Credential.User.Status.Should().Be(EndUserStatus.Registered);
        result.Content.Value.Credential.User.Classification.Should().Be(EndUserClassification.Person);
        result.Content.Value.Credential.User.Roles.Should().BeEmpty();
        result.Content.Value.Credential.User.FeatureLevels.Should().BeEmpty();
        result.Content.Value.Credential.User.Profile!.Id.Should().Be(result.Content.Value.Credential.User.Id);
        result.Content.Value.Credential.User.Profile!.DefaultOrganisationId.Should().BeNull();
        result.Content.Value.Credential.User.Profile!.Name.FirstName.Should().Be("afirstname");
        result.Content.Value.Credential.User.Profile!.Name.LastName.Should().Be("alastname");
        result.Content.Value.Credential.User.Profile!.DisplayName.Should().Be("afirstname");
        result.Content.Value.Credential.User.Profile!.EmailAddress.Should().Be("auser@company.com");
        result.Content.Value.Credential.User.Profile!.Timezone.Should().Be(Timezones.Default.ToString());
        result.Content.Value.Credential.User.Profile!.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
    }

    [Fact]
    public async Task WhenAuthenticateAndUserNotExists_ThenReturnsUnAuthorized()
    {
        var result = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = "auser@company.com",
            Password = "Password1!"
        });

        result.Content.Error.Status.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndUserExists_ThenReturnsTokens()
    {
        await Api.PostAsync(new RegisterPersonRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        var token = _notificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmPersonRegistrationRequest
        {
            Token = token!
        });

        var result = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        result.Content.Value.AccessToken.Should().NotBeNull();
        result.Content.Value.RefreshToken.Should().NotBeNull();
        result.Content.Value.ExpiresOnUtc.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.DefaultAccessTokenExpiry));
    }

    [Fact]
    public async Task WhenRefreshToken_ThenReturnsNewTokens()
    {
        await Api.PostAsync(new RegisterPersonRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        var token = _notificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmPersonRegistrationRequest
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