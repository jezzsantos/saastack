using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EndUsersInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class InvitationsApiSpec : WebApiSpec<Program>
{
    private static int _invitationCount;
    private readonly StubUserNotificationsService _userNotificationService;

    public InvitationsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _userNotificationService =
            setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
        _userNotificationService.Reset();
    }

    [Fact]
    public async Task WhenInviteGuestAndNotYetInvited_ThenInvites()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        var result = await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Invitation.EmailAddress.Should().Be(emailAddress);
        result.Content.Value.Invitation.FirstName.Should().Be("Aninvitee");
        result.Content.Value.Invitation.LastName.Should().BeNull();
        _userNotificationService.LastGuestInvitationEmailRecipient.Should().Be(emailAddress);
    }

    [Fact]
    public async Task WhenInviteGuestAndAlreadyInvited_ThenReInvites()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));

        // Delay to allow for relevant timestamp checks
        await Task.Delay(TimeSpan.FromSeconds(2));
        _userNotificationService.Reset();

        var result = await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Invitation.EmailAddress.Should().Be(emailAddress);
        result.Content.Value.Invitation.FirstName.Should().Be("Aninvitee");
        result.Content.Value.Invitation.LastName.Should().BeNull();
        _userNotificationService.LastGuestInvitationEmailRecipient.Should().Be(emailAddress);
    }

    [Fact]
    public async Task WhenInviteUserAsGuestAndAlreadyRegistered_ThenDoesNothing()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));

        await RegisterUserAsync(emailAddress);

        // Delay to allow for relevant timestamp checks
        await Task.Delay(TimeSpan.FromSeconds(2));
        _userNotificationService.Reset();

        var result = await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Invitation.EmailAddress.Should().Be(emailAddress);
        result.Content.Value.Invitation.FirstName.Should().Be("afirstname");
        result.Content.Value.Invitation.LastName.Should().Be("alastname");
        _userNotificationService.LastGuestInvitationEmailRecipient.Should().BeNull();
    }

    [Fact]
    public async Task WhenVerifyInvitationAndRegistered_ThenReturnsError()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        await RegisterUserAsync(emailAddress);

        var result = await Api.GetAsync(new VerifyGuestInvitationRequest
        {
            Token = token
        });

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenVerifyInvitationAndInvited_ThenVerifies()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        var result = await Api.GetAsync(new VerifyGuestInvitationRequest
        {
            Token = token
        });

        result.Content.Value.Invitation.EmailAddress.Should().Be(emailAddress);
        result.Content.Value.Invitation.FirstName.Should().Be("Aninvitee");
        result.Content.Value.Invitation.LastName.Should().BeNull();
    }

    [Fact]
    public async Task WhenResendInvitationAndRegistered_ThenReturnsError()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        await RegisterUserAsync(emailAddress);

        var result = await Api.PostAsync(new ResendGuestInvitationRequest
        {
            Token = token
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenResendInvitationAndInvited_ThenResends()
    {
        var login = await LoginUserAsync();
        var emailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = emailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;
        _userNotificationService.Reset();

        await Api.PostAsync(new ResendGuestInvitationRequest
        {
            Token = token
        }, req => req.SetJWTBearerToken(login.AccessToken));

        _userNotificationService.LastGuestInvitationEmailRecipient.Should().Be(emailAddress);
    }

    [Fact]
    public async Task WhenAcceptInvitationAndNotInvited_ThenRegistersUser()
    {
        var acceptedEmailAddress = CreateRandomEmailAddress();

        var result = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            InvitationToken = new TokensService().CreateGuestInvitationToken(),
            EmailAddress = acceptedEmailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Credential.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Access.Should().Be(EndUserAccess.Enabled);
        result.Content.Value.Credential.User.Status.Should().Be(EndUserStatus.Registered);
        result.Content.Value.Credential.User.Classification.Should().Be(EndUserClassification.Person);
        result.Content.Value.Credential.User.Roles.Should().OnlyContain(rol => rol == PlatformRoles.Standard.Name);
        result.Content.Value.Credential.User.Features.Should()
            .ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
    }

    [Fact]
    public async Task WhenAcceptInvitationAndInvitedOnSameEmailAddress_ThenRegistersUser()
    {
        var login = await LoginUserAsync();
        var invitedEmailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = invitedEmailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        var result = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            InvitationToken = token,
            EmailAddress = invitedEmailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Credential.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Access.Should().Be(EndUserAccess.Enabled);
        result.Content.Value.Credential.User.Status.Should().Be(EndUserStatus.Registered);
        result.Content.Value.Credential.User.Classification.Should().Be(EndUserClassification.Person);
        result.Content.Value.Credential.User.Roles.Should().OnlyContain(rol => rol == PlatformRoles.Standard.Name);
        result.Content.Value.Credential.User.Features.Should()
            .ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
    }

    [Fact]
    public async Task WhenAcceptInvitationAndInvitedOnDifferentEmailAddress_ThenRegistersUser()
    {
        var login = await LoginUserAsync();
        var invitedEmailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = invitedEmailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        var registeredEmailAddress = CreateRandomEmailAddress();
        var result = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            InvitationToken = token,
            EmailAddress = registeredEmailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.Content.Value.Credential.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Id.Should().NotBeEmpty();
        result.Content.Value.Credential.User.Access.Should().Be(EndUserAccess.Enabled);
        result.Content.Value.Credential.User.Status.Should().Be(EndUserStatus.Registered);
        result.Content.Value.Credential.User.Classification.Should().Be(EndUserClassification.Person);
        result.Content.Value.Credential.User.Roles.Should().OnlyContain(rol => rol == PlatformRoles.Standard.Name);
        result.Content.Value.Credential.User.Features.Should()
            .ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
    }

    [Fact]
    public async Task WhenAcceptInvitationOnAnExistingEmailAddress_ThenReturnsError()
    {
        var login = await LoginUserAsync();
        var invitedEmailAddress = CreateRandomEmailAddress();

        await Api.PostAsync(new InviteGuestRequest
        {
            Email = invitedEmailAddress
        }, req => req.SetJWTBearerToken(login.AccessToken));
        var token = _userNotificationService.LastGuestInvitationToken!;

        var existingEmailAddress = login.Profile!.EmailAddress!;
        var result = await Api.PostAsync(new RegisterPersonPasswordRequest
        {
            InvitationToken = token,
            EmailAddress = existingEmailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static string CreateRandomEmailAddress()
    {
        return $"aninvitee{++_invitationCount}@company.com";
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // Override dependencies here
    }
}