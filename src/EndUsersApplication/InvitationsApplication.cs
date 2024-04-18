using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using Membership = Application.Resources.Shared.Membership;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace EndUsersApplication;

public partial class InvitationsApplication : IInvitationsApplication
{
    private readonly IIdentifierFactory _idFactory;
    private readonly IInvitationRepository _repository;
    private readonly INotificationsService _notificationsService;
    private readonly IRecorder _recorder;
    private readonly ITokensService _tokensService;
    private readonly IUserProfilesService _userProfilesService;

    public InvitationsApplication(IRecorder recorder, IIdentifierFactory idFactory, ITokensService tokensService,
        INotificationsService notificationsService, IUserProfilesService userProfilesService,
        IInvitationRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _tokensService = tokensService;
        _notificationsService = notificationsService;
        _userProfilesService = userProfilesService;
        _repository = repository;
    }

    public async Task<Result<Invitation, Error>> InviteGuestAsync(ICallerContext context, string emailAddress,
        CancellationToken cancellationToken)
    {
        var invited = await InviteGuestByEmailInternalAsync(context, context.CallerId, emailAddress, cancellationToken);
        if (!invited.IsSuccessful)
        {
            return invited.Error;
        }

        var invitee = invited.Value.Invitee;
        var saved = await _repository.SaveAsync(invitee, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Guest {Id} was invited", invitee.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.GuestInvited,
            new Dictionary<string, object>
            {
                { nameof(EndUserRoot.Id), invitee.Id },
                { nameof(UserProfile.EmailAddress), emailAddress }
            });

        return invited.Value.Invitee.ToInvitation(invited.Value.Profile);
    }

    public async Task<Result<Error>> ResendGuestInvitationAsync(ICallerContext context, string token,
        CancellationToken cancellationToken)
    {
        var retrievedInviter = await _repository.LoadAsync(context.ToCallerId(), cancellationToken);
        if (!retrievedInviter.IsSuccessful)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;

        var retrievedGuest = await _repository.FindInvitedGuestByTokenAsync(token, cancellationToken);
        if (!retrievedGuest.IsSuccessful)
        {
            return retrievedGuest.Error;
        }

        if (!retrievedGuest.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var invitee = retrievedGuest.Value.Value;

        var invited = await invitee.ReInviteGuestAsync(_tokensService, inviter.Id,
            async (inviterId, newToken) =>
                await SendInvitationNotificationAsync(context, inviterId, newToken, invitee, cancellationToken));
        if (!invited.IsSuccessful)
        {
            return invited.Error;
        }

        var saved = await _repository.SaveAsync(invitee, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Guest {Id} was re-invited", invitee.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.GuestInvited,
            new Dictionary<string, object>
            {
                { nameof(EndUserRoot.Id), invitee.Id },
                { nameof(UserProfile.EmailAddress), invitee.GuestInvitation.InviteeEmailAddress!.Address }
            });

        return Result.Ok;
    }

    public async Task<Result<Invitation, Error>> VerifyGuestInvitationAsync(ICallerContext context, string token,
        CancellationToken cancellationToken)
    {
        var retrievedGuest = await _repository.FindInvitedGuestByTokenAsync(token, cancellationToken);
        if (!retrievedGuest.IsSuccessful)
        {
            return retrievedGuest.Error;
        }

        if (!retrievedGuest.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var invitee = retrievedGuest.Value.Value;
        var verified = invitee.VerifyGuestInvitation();
        if (!verified.IsSuccessful)
        {
            return verified.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Guest {Id} invitation was verified", invitee.Id);
        return invitee.ToInvitation();
    }

    private async Task<Result<Membership, Error>> InviteMemberToOrganizationAsync(ICallerContext context,
        string organizationId, string invitedById, string? userId, string? emailAddress,
        CancellationToken cancellationToken)
    {
        if (emailAddress.HasNoValue() && userId.HasNoValue())
        {
            return Error.RuleViolation(Resources
                .InvitationsApplication_InviteMemberToOrganization_NoUserIdNorEmailAddress);
        }

        var inviter = await _repository.LoadAsync(invitedById.ToId(), cancellationToken);
        if (!inviter.IsSuccessful)
        {
            return inviter.Error;
        }

        EndUserRoot invitee = null!;
        if (emailAddress.HasValue())
        {
            var invited = await InviteGuestByEmailInternalAsync(context, invitedById, emailAddress, cancellationToken);
            if (!invited.IsSuccessful)
            {
                return invited.Error;
            }

            invitee = invited.Value.Invitee;
        }

        if (userId.HasValue())
        {
            var invited = await InviteGuestByUserIdInternalAsync(context, invitedById, userId, cancellationToken);
            if (!invited.IsSuccessful)
            {
                return invited.Error;
            }

            invitee = invited.Value.Invitee;
        }

        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.InvitingMemberToOrg,
                context.IsAuthenticated);
        var enrolled = invitee.AddMembership(inviter.Value, OrganizationOwnership.Shared, organizationId.ToId(),
            tenantRoles, tenantFeatures);
        if (!enrolled.IsSuccessful)
        {
            return enrolled.Error;
        }

        var membership = invitee.FindMembership(organizationId.ToId());
        var saved = await _repository.SaveAsync(invitee, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(),
            "EndUser {Id} has been invited to organization {Organization}", saved.Value.Id, organizationId);

        return membership.Value.ToMembership();
    }

    private async Task<Result<(EndUserRoot Invitee, UserProfile? Profile), Error>> InviteGuestByEmailInternalAsync(
        ICallerContext context, string invitedById, string emailAddress, CancellationToken cancellationToken)
    {
        var retrievedInviter = await _repository.LoadAsync(invitedById.ToId(), cancellationToken);
        if (!retrievedInviter.IsSuccessful)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;

        var email = EmailAddress.Create(emailAddress);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var retrievedGuest =
            await _repository.FindInvitedGuestByEmailAddressAsync(email.Value, cancellationToken);
        if (!retrievedGuest.IsSuccessful)
        {
            return retrievedGuest.Error;
        }

        EndUserRoot invitee;
        if (retrievedGuest.Value.HasValue)
        {
            invitee = retrievedGuest.Value.Value;
            if (invitee.Status == UserStatus.Registered)
            {
                return Error.EntityExists(Resources.EndUsersApplication_GuestAlreadyRegistered);
            }
        }
        else
        {
            var retrievedEmailOwner =
                await _userProfilesService.FindPersonByEmailAddressPrivateAsync(context, emailAddress,
                    cancellationToken);
            if (!retrievedEmailOwner.IsSuccessful)
            {
                return retrievedEmailOwner.Error;
            }

            if (retrievedEmailOwner.Value.HasValue)
            {
                var retrievedInvitee =
                    await _repository.LoadAsync(retrievedEmailOwner.Value.Value.UserId.ToId(),
                        cancellationToken);
                if (!retrievedInvitee.IsSuccessful)
                {
                    return retrievedInvitee.Error;
                }

                return (retrievedInvitee.Value, retrievedEmailOwner.Value.Value);
            }

            var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            invitee = created.Value;
        }

        var invited = await invitee.InviteGuestAsync(_tokensService, inviter.Id, email.Value,
            async (inviterId, newToken) =>
                await SendInvitationNotificationAsync(context, inviterId, newToken, invitee, cancellationToken));
        if (!invited.IsSuccessful)
        {
            return invited.Error;
        }

        return (invitee, null);
    }

    private async Task<Result<(EndUserRoot Invitee, UserProfile? Profile), Error>> InviteGuestByUserIdInternalAsync(
        ICallerContext context, string invitedById,
        string userId, CancellationToken cancellationToken)
    {
        var retrievedInviter = await _repository.LoadAsync(invitedById.ToId(), cancellationToken);
        if (!retrievedInviter.IsSuccessful)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;

        var retrievedInvitee = await _repository.LoadAsync(userId.ToId(), cancellationToken);
        if (!retrievedInvitee.IsSuccessful)
        {
            return retrievedInvitee.Error;
        }

        var invitee = retrievedInvitee.Value;
        if (invitee.IsRegistered)
        {
            return (invitee, null);
        }

        var email = EmailAddress.Create(invitee.GuestInvitation.InviteeEmailAddress!.Address);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var invited = await invitee.InviteGuestAsync(_tokensService, inviter.Id, email.Value,
            async (inviterId, newToken) =>
                await SendInvitationNotificationAsync(context, inviterId, newToken, invitee, cancellationToken));
        if (!invited.IsSuccessful)
        {
            return invited.Error;
        }

        return (invitee, null);
    }

    private async Task<Result<Error>> SendInvitationNotificationAsync(ICallerContext context,
        Identifier inviterId, string token, EndUserRoot invitee, CancellationToken cancellationToken)
    {
        var inviterProfile =
            await _userProfilesService.GetProfilePrivateAsync(context, inviterId, cancellationToken);
        if (!inviterProfile.IsSuccessful)
        {
            return inviterProfile.Error;
        }

        var inviteeEmailAddress = invitee.GuestInvitation.InviteeEmailAddress!.Address;
        var inviteeName = invitee.GuessGuestInvitationName().FirstName;
        var inviterName = inviterProfile.Value.DisplayName;
        var notified =
            await _notificationsService.NotifyGuestInvitationToPlatformAsync(context, token, inviteeEmailAddress,
                inviteeName, inviterName, cancellationToken);
        if (!notified.IsSuccessful)
        {
            return notified.Error;
        }

        return Result.Ok;
    }
}

internal static class InvitationConversionExtensions
{
    public static Invitation ToInvitation(this EndUserRoot invitee, UserProfile? profile = null)
    {
        if (profile.Exists())
        {
            return new Invitation
            {
                EmailAddress = profile.EmailAddress!,
                FirstName = profile.Name.FirstName,
                LastName = profile.Name.LastName
            };
        }

        var assumedName = invitee.GuessGuestInvitationName();
        return new Invitation
        {
            EmailAddress = invitee.GuestInvitation.InviteeEmailAddress!.Address,
            FirstName = assumedName.FirstName,
            LastName = assumedName.LastName.ValueOrDefault!
        };
    }
}