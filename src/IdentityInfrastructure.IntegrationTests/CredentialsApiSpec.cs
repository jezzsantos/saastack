using System.Net;
using System.Text.Json;
using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using IdentityApplication;
using IdentityDomain;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class CredentialsApiSpec : WebApiSpec<Program>
{
    private static int _userCount;
    private readonly StubUserNotificationsService _userNotificationsService;

    public CredentialsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _userNotificationsService =
            setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
        _userNotificationsService.Reset();
    }

    [Fact]
    public async Task WhenRegisterPersonWithNewEmail_ThenRegistersNewUser()
    {
        var result = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Person.Id.Should().NotBeEmpty();
        result.Content.Value.Person.User.Id.Should().NotBeEmpty();
        result.Content.Value.Person.User.Access.Should().Be(EndUserAccess.Enabled);
        result.Content.Value.Person.User.Status.Should().Be(EndUserStatus.Registered);
        result.Content.Value.Person.User.Classification.Should().Be(EndUserClassification.Person);
        result.Content.Value.Person.User.Roles.Should().OnlyContain(rol => rol == PlatformRoles.Standard.Name);
        result.Content.Value.Person.User.Features.Should()
            .ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();
    }

    [Fact]
    public async Task
        WhenRegisterWithSameEmailAndEndUserAlreadyRegisteredWithPassword_ThenRegistersAndSendsCourtesyEmail()
    {
        const string emailAddress = "auser@company.com";
        var registered = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        registered.Content.Value.Person.User.Id.Should().NotBeNull();
        var userId = registered.Content.Value.Person.User.Id;

        await PropagateDomainEventsAsync(PropagationRounds.Twice);
        var result = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Person.User.Id.Should().Be(userId);
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().Be(emailAddress);
    }

    [Fact]
    public async Task WhenRegisterWithSameEmailAndEndUserAlreadyRegisteredWithSSO_ThenRegistersAndNoCourtesyEmail()
    {
        const string emailAddress = "auser@company.com";
        var registered = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = emailAddress,
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        registered.Content.Value.Tokens.UserId.Should().NotBeNull();
        var userId = registered.Content.Value.Tokens.UserId;

        await PropagateDomainEventsAsync(PropagationRounds.Twice);
        var result = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Person.User.Id.Should().Be(userId);
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();
    }

    [Fact]
    public async Task WhenAuthenticateAndUserNotExists_ThenReturnsUnAuthorized()
    {
        var result = await Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = "auser@company.com",
            Password = "Password1!"
        });

        result.Content.Error.Status.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndUserExists_ThenReturnsTokens()
    {
        await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        await PropagateDomainEventsAsync();
        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonCredentialRequest
        {
            Token = token!
        });

        await PropagateDomainEventsAsync();
        var result = await Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
    }

    [Fact]
    public async Task WhenAuthenticateAndMfaEnabled_ThenReturnsMfaContinuance()
    {
        var login = await LoginUserAsync();
        await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
        {
            IsEnabled = true
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = login.Profile!.EmailAddress,
            Password = PasswordForPerson
        });

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be(PersonCredentialsApplication.MfaRequiredCode);
        result.Content.Error.Extensions!.Count.Should().Be(1);
        result.Content.Error.Extensions[PersonCredentialsApplication.MfaTokenName].As<JsonElement>().GetString()
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenCallingSecureApiAfterAuthenticate_ThenReturnsResponse()
    {
        var person = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        await PropagateDomainEventsAsync();
        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmRegistrationPersonCredentialRequest
        {
            Token = token!
        });

        await PropagateDomainEventsAsync();
        var authenticate = await Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        });

        var accessToken = authenticate.Content.Value.Tokens.AccessToken.Value;

#if TESTINGONLY
        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetJWTBearerToken(accessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(person.Content.Value.Person.User.Id);
#endif
    }

    [Fact]
    public async Task WhenInitiatePasswordResetForUnregisteredEmailAddress_ThenSendsCourtesyEmail()
    {
        var result = await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = "anunknownuser@company.com"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _userNotificationsService.LastPasswordResetCourtesyEmailRecipient.Should().Be("anunknownuser@company.com");
    }

    [Fact]
    public async Task WhenInitiatePasswordResetAndRegistrationNotVerified_ThenReturnsError()
    {
        var emailAddress = CreateRandomEmailAddress();
        await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        var result = await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = emailAddress
        });

        result.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task WhenInitiatePasswordReset_ThenSendsEmailNotification()
    {
        var login = await LoginUserAsync();

        var emailAddress = login.Profile!.EmailAddress!;
        var result = await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = emailAddress
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _userNotificationsService.LastPasswordResetEmailRecipient.Should().Be(emailAddress);
        _userNotificationsService.LastPasswordResetToken.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenResendPasswordReset_ThenResendsEmailNotification()
    {
        var login = await LoginUserAsync();

        await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = login.Profile!.EmailAddress!
        });

        var token = _userNotificationsService.LastPasswordResetToken;
        _userNotificationsService.Reset();

        var result = await Api.PostAsync(new ResendPasswordResetRequest
        {
            Token = token!
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _userNotificationsService.LastPasswordResetEmailRecipient.Should().Be(login.Profile.EmailAddress);
        _userNotificationsService.LastPasswordResetToken.Should().NotBe(token);
    }

    [Fact]
    public async Task WhenVerifyPasswordReset_ThenConfirms()
    {
        var login = await LoginUserAsync();

        await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = login.Profile!.EmailAddress!
        });

        var token = _userNotificationsService.LastPasswordResetToken;
        var result = await Api.GetAsync(new VerifyPasswordResetRequest
        {
            Token = token!
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenCompletePasswordResetWithUnknownToken_ThenReturnsError()
    {
        var token = new TokensService().CreatePasswordResetToken();
        var result = await Api.PostAsync(new CompleteCredentialResetRequest
        {
            Token = token,
            Password = "a1Password!"
        });

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenCompletePasswordResetWithLockedAccount_ThenUnlocksAccountAndResets()
    {
        var login = await LoginUserAsync();
        LockAccountWithFailedLogins(login);

        var emailAddress = login.Profile!.EmailAddress!;
        await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = emailAddress
        });

        var token = _userNotificationsService.LastPasswordResetToken!;
        await Api.PostAsync(new CompleteCredentialResetRequest
        {
            Token = token,
            Password = "2Password!"
        });

        var authenticated = await ReAuthenticateUserAsync(login.User, emailAddress, "2Password!");

        authenticated.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WhenCompletePasswordReset_ThenResets()
    {
        var login = await LoginUserAsync();

        var emailAddress = login.Profile!.EmailAddress!;
        await Api.PostAsync(new InitiatePasswordResetRequest
        {
            EmailAddress = emailAddress
        });

        var token = _userNotificationsService.LastPasswordResetToken!;
        await Api.PostAsync(new CompleteCredentialResetRequest
        {
            Token = token,
            Password = "2Password!"
        });

        var authenticated = await ReAuthenticateUserAsync(login.User, emailAddress, "2Password!");

        authenticated.AccessToken.Should().NotBeNullOrEmpty();
    }

    private void LockAccountWithFailedLogins(LoginDetails login,
        int wrongAttempts = Validations.Credentials.Login.DefaultMaxFailedPasswordAttempts)
    {
        var emailAddress = login.Profile!.EmailAddress!;
        Repeat.Times(() => Try.Safely(() => Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = emailAddress,
            Password = "awrongpassword"
        })), wrongAttempts);
    }

    private static string CreateRandomEmailAddress()
    {
        return $"auser{++_userCount}@company.com";
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}