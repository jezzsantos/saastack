using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class PasswordCredentialsApiSpec : WebApiSpec<Program>
{
    public PasswordCredentialsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
    }

    [Fact]
    public async Task WhenRegisterPerson_ThenRegisters()
    {
        var result = await Api.PostAsync(new RegisterPersonPasswordRequest
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
    public async Task WhenCallingSecureApiAfterAuthenticate_ThenReturnsResponse()
    {
        var person = await Api.PostAsync(new RegisterPersonPasswordRequest
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

        var authenticate = await Api.PostAsync(new AuthenticatePasswordRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        var accessToken = authenticate.Content.Value.AccessToken!;

#if TESTINGONLY
        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetJWTBearerToken(accessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(person.Content.Value.Credential!.User.Id);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}