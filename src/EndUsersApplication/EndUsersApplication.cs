using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using EndUser = Application.Resources.Shared.EndUser;
using Membership = Application.Resources.Shared.Membership;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication;

public partial class EndUsersApplication : IEndUsersApplication
{
    internal const string PermittedOperatorsSettingName = "Hosts:EndUsersApi:Authorization:OperatorWhitelist";
    private static readonly char[] PermittedOperatorsDelimiters = [';', ',', ' '];
    private readonly IEndUserRepository _endUserRepository;
    private readonly IIdentifierFactory _idFactory;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IRecorder _recorder;
    private readonly IConfigurationSettings _settings;
    private readonly IUserNotificationsService _userNotificationsService;
    private readonly IUserProfilesService _userProfilesService;

    public EndUsersApplication(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IUserNotificationsService userNotificationsService, IUserProfilesService userProfilesService,
        IInvitationRepository invitationRepository,
        IEndUserRepository endUserRepository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _settings = settings;
        _userNotificationsService = userNotificationsService;
        _userProfilesService = userProfilesService;
        _invitationRepository = invitationRepository;
        _endUserRepository = endUserRepository;
    }

    public async Task<Result<EndUser, Error>> GetUserAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(caller.ToCall(), "Retrieved user: {Id}", user.Id);

        return user.ToUser();
    }

    public async Task<Result<SearchResults<Membership>, Error>> ListMembershipsForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var userId = caller.ToCallerId();
        var retrieved = await _endUserRepository.LoadAsync(userId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;
        var memberships = user.Memberships.Select(ms => ms.ToMembership()).ToList();

        _recorder.TraceInformation(caller.ToCall(), "Retrieved memberships for user: {Id}", user.Id);

        return searchOptions.ApplyWithMetadata(memberships);
    }

    public async Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller, string organizationId, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var retrieved =
            await _endUserRepository.SearchAllMembershipsByOrganizationAsync(organizationId.ToId(), searchOptions,
                cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var members = retrieved.Value;
        if (!IsMember(caller.ToCallerId(), members))
        {
            return Error.ForbiddenAccess(Resources.EndUsersApplication_CallerNotMember);
        }

        return searchOptions.ApplyWithMetadata(
            await WithGetOptionsAsync(caller, members, getOptions, cancellationToken));
    }

    public async Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(caller.ToCall(), "Retrieved user with  memberships: {Id}", user.Id);

        return user.ToUserWithMemberships();
    }

    public async Task<Result<EndUser, Error>> RegisterMachineAsync(ICallerContext caller, string name,
        string? timezone, string? countryCode, CancellationToken cancellationToken)
    {
        var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Machine);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var userProfile = EndUserProfile.Create(name, timezone: timezone, countryCode: countryCode);
        if (userProfile.IsFailure)
        {
            return userProfile.Error;
        }

        var machine = created.Value;
        var (platformRoles, platformFeatures, _, _) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.CreatingMachine, caller.IsAuthenticated);
        var registered =
            machine.Register(platformRoles, platformFeatures, userProfile.Value, Optional<EmailAddress>.None);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(machine, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        machine = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Registered machine: {Id}", machine.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.MachineRegistered);

        if (caller.IsAuthenticated)
        {
            var retrievedAdder = await _endUserRepository.LoadAsync(caller.ToCallerId(), cancellationToken);
            if (retrievedAdder.IsFailure)
            {
                return retrievedAdder.Error;
            }

            var adder = retrievedAdder.Value;
            var adderDefaultMembership = adder.DefaultMembership;
            if (adderDefaultMembership.IsShared)
            {
                var (_, _, tenantRoles2, tenantFeatures2) =
                    EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.InvitingMachineToCreatorOrg,
                        caller.IsAuthenticated);
                var adderEnrolled = machine.AddMembership(adder, adderDefaultMembership.Ownership,
                    adderDefaultMembership.OrganizationId, tenantRoles2, tenantFeatures2);
                if (adderEnrolled.IsFailure)
                {
                    return adderEnrolled.Error;
                }

                saved = await _endUserRepository.SaveAsync(machine, cancellationToken);
                if (saved.IsFailure)
                {
                    return saved.Error;
                }

                machine = saved.Value;
                _recorder.TraceInformation(caller.ToCall(),
                    "Machine {Id} has become a member of {User} organization {Organization}",
                    machine.Id, adder.Id, adderDefaultMembership.OrganizationId);
            }
        }

        return machine.ToUser();
    }

    public async Task<Result<EndUser, Error>> RegisterPersonAsync(ICallerContext caller,
        string? invitationToken, string emailAddress, string firstName, string? lastName, string? timezone,
        string? countryCode, bool termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        if (!termsAndConditionsAccepted)
        {
            return Error.RuleViolation(Resources.EndUsersApplication_NotAcceptedTerms);
        }

        var email = EmailAddress.Create(emailAddress);
        if (email.IsFailure)
        {
            return email.Error;
        }

        var username = email.Value;

        var existingUser = Optional<EndUserWithProfile>.None;
        if (invitationToken.HasValue())
        {
            var retrievedGuest =
                await FindInvitedGuestWithInvitationTokenAsync(invitationToken, cancellationToken);
            if (retrievedGuest.IsFailure)
            {
                return retrievedGuest.Error;
            }

            if (retrievedGuest.Value.HasValue)
            {
                var existingRegisteredUser =
                    await FindProfileWithEmailAddressAsync(caller, username, cancellationToken);
                if (existingRegisteredUser.IsFailure)
                {
                    return existingRegisteredUser.Error;
                }

                if (existingRegisteredUser.Value.HasValue)
                {
                    return Error.EntityExists(Resources.EndUsersApplication_AcceptedInvitationWithExistingEmailAddress);
                }

                var invitee = retrievedGuest.Value.Value;
                var acceptedById = caller.ToCallerId();
                var accepted = invitee.AcceptGuestInvitation(acceptedById, username);
                if (accepted.IsFailure)
                {
                    return accepted.Error;
                }

                _recorder.TraceInformation(caller.ToCall(), "Guest user {Id} accepted their invitation", invitee.Id);
                existingUser = new EndUserWithProfile(invitee, null);
            }
        }

        if (!existingUser.HasValue)
        {
            var registeredOrGuest =
                await FindRegisteredPersonOrInvitedGuestByEmailAddressAsync(caller, username, cancellationToken);
            if (registeredOrGuest.IsFailure)
            {
                return registeredOrGuest.Error;
            }

            existingUser = registeredOrGuest.Value;
        }

        EndUserRoot unregisteredUser;
        if (existingUser.HasValue)
        {
            unregisteredUser = existingUser.Value.User;

            if (unregisteredUser.Status == UserStatus.Registered)
            {
                var unregisteredUserProfile = existingUser.Value.Profile;
                if (unregisteredUserProfile.NotExists()
                    || unregisteredUserProfile.Classification != UserProfileClassification.Person
                    || unregisteredUserProfile.EmailAddress.HasNoValue())
                {
                    return Error.EntityNotFound(Resources.EndUsersApplication_NotPersonProfile);
                }

                var notified = await _userNotificationsService.NotifyPasswordRegistrationRepeatCourtesyAsync(caller,
                    unregisteredUser.Id, unregisteredUserProfile.EmailAddress, unregisteredUserProfile.DisplayName,
                    unregisteredUserProfile.Timezone, unregisteredUserProfile.Address.CountryCode, cancellationToken);
                if (notified.IsFailure)
                {
                    return notified.Error;
                }

                _recorder.TraceInformation(caller.ToCall(),
                    "Attempted re-registration of user: {Id}, with email {EmailAddress}", unregisteredUser.Id, email);
                _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.PersonReRegistered,
                    new Dictionary<string, object>
                    {
                        { UsageConstants.Properties.Id, unregisteredUser.Id },
                        { UsageConstants.Properties.EmailAddress, username.Address }
                    });

                return unregisteredUser.ToUser();
            }
        }
        else
        {
            var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
            if (created.IsFailure)
            {
                return created.Error;
            }

            unregisteredUser = created.Value;
        }

        var userProfile = EndUserProfile.Create(firstName, lastName, timezone, countryCode);
        if (userProfile.IsFailure)
        {
            return userProfile.Error;
        }

        var permittedOperators = GetPermittedOperators();
        var (platformRoles, platformFeatures, _, _) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.CreatingPerson, caller.IsAuthenticated,
                username, permittedOperators);
        var registered = unregisteredUser.Register(platformRoles, platformFeatures, userProfile.Value, username);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(unregisteredUser, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var person = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Registered user: {Id}", person.Id);
        _recorder.AuditAgainst(caller.ToCall(), person.Id,
            Audits.EndUsersApplication_User_Registered_TermsAccepted,
            "EndUser {Id} accepted their terms and conditions", person.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated);

        return person.ToUser();
    }

    public async Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAddressAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(emailAddress);
        if (email.IsFailure)
        {
            return email.Error;
        }

        var retrieved =
            await FindRegisteredPersonOrInvitedGuestByEmailAddressAsync(caller, email.Value, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        return retrieved.Value.HasValue
            ? retrieved.Value.Value.User.ToUser().ToOptional()
            : Optional<EndUser>.None;
    }

    public async Task<Result<EndUser, Error>> AssignPlatformRolesAsync(ICallerContext caller, string id,
        List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrievedAssignee.IsFailure)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _endUserRepository.LoadAsync(caller.ToCallerId(), cancellationToken);
        if (retrievedAssigner.IsFailure)
        {
            return retrievedAssigner.Error;
        }

        var assignee = retrievedAssignee.Value;
        var assigner = retrievedAssigner.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (assigneeRoles.IsFailure)
        {
            return assigneeRoles.Error;
        }

        var assigned = assignee.AssignPlatformRoles(assigner, assigneeRoles.Value);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        var saved = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        assignee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been assigned platform roles {Roles}",
            assignee.Id, roles.JoinAsOredChoices());
        _recorder.AuditAgainst(caller.ToCall(), assignee.Id,
            Audits.EndUserApplication_PlatformRolesAssigned,
            "EndUser {AssignerId} assigned the platform roles {Roles} to assignee {AssigneeId}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id);

        return assignee.ToUser();
    }

    public async Task<Result<EndUser, Error>> UnassignPlatformRolesAsync(ICallerContext caller, string id,
        List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrievedAssignee.IsFailure)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _endUserRepository.LoadAsync(caller.ToCallerId(), cancellationToken);
        if (retrievedAssigner.IsFailure)
        {
            return retrievedAssigner.Error;
        }

        var assignee = retrievedAssignee.Value;
        var assigner = retrievedAssigner.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (assigneeRoles.IsFailure)
        {
            return assigneeRoles.Error;
        }

        var unassigned = assignee.UnassignPlatformRoles(assigner, assigneeRoles.Value);
        if (unassigned.IsFailure)
        {
            return unassigned.Error;
        }

        var saved = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        assignee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been unassigned platform roles {Roles}",
            assignee.Id, roles.JoinAsOredChoices());
        _recorder.AuditAgainst(caller.ToCall(), assignee.Id,
            Audits.EndUserApplication_PlatformRolesUnassigned,
            "EndUser {AssignerId} unassigned the platform roles {Roles} from assignee {AssigneeId}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id);

        return assignee.ToUser();
    }

    public async Task<Result<EndUser, Error>> ChangeDefaultMembershipAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken)
    {
        var userId = caller.ToCallerId();
        var retrieved = await _endUserRepository.LoadAsync(userId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;
        var changed = user.ChangeDefaultMembership(organizationId.ToId());
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _endUserRepository.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        user = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Default membership changed for user {Id} to {OrganizationId}",
            user.Id, organizationId);

        return user.ToUser();
    }

    private async Task<IEnumerable<MembershipWithUserProfile>> WithGetOptionsAsync(ICallerContext caller,
        List<MembershipJoinInvitation> memberships, GetOptions options, CancellationToken cancellationToken)
    {
        var ids = memberships
            .Where(membership => membership.Status.Value.ToEnumOrDefault(EndUserStatus.Unregistered)
                                 == EndUserStatus.Registered)
            .Select(membership => membership.UserId.Value).ToList();

        var profiles = new List<UserProfile>();
        if (ids.Count > 0)
        {
            var retrieved =
                await _userProfilesService.GetAllProfilesPrivateAsync(caller, ids, options, cancellationToken);
            if (retrieved.IsSuccessful)
            {
                profiles = retrieved.Value;
            }
        }

        return memberships.ConvertAll(membership =>
        {
            // These can be a person with a profile, or a machine without a profile
            var member = membership.ToMembership();
            var status = membership.Status.Value.ToEnumOrDefault(EndUserStatus.Unregistered);
            var profile = profiles.FirstOrDefault(profile => profile.UserId == membership.UserId);

            member.Profile = (status == EndUserStatus.Unregistered
                ? membership.ToUnregisteredUserProfile()
                : profile)!;

            return member;
        });
    }

    private static bool IsMember(Identifier userId, List<MembershipJoinInvitation> members)
    {
        return members.Any(ms => ms.UserId.Value.EqualsIgnoreCase(userId));
    }

    private async Task<Result<Optional<EndUserWithProfile>, Error>>
        FindRegisteredPersonOrInvitedGuestByEmailAddressAsync(ICallerContext caller, EmailAddress emailAddress,
            CancellationToken cancellationToken)
    {
        var existingProfile = await FindProfileWithEmailAddressAsync(caller, emailAddress, cancellationToken);
        if (existingProfile.IsFailure)
        {
            return existingProfile.Error;
        }

        if (existingProfile.Value.HasValue)
        {
            return existingProfile;
        }

        var existingInvitation = await FindInvitedGuestWithEmailAddressAsync(emailAddress, cancellationToken);
        if (existingInvitation.IsFailure)
        {
            return existingInvitation.Error;
        }

        if (existingInvitation.Value.HasValue)
        {
            return existingInvitation;
        }

        return Optional<EndUserWithProfile>.None;
    }

    private async Task<Result<Optional<EndUserWithProfile>, Error>> FindProfileWithEmailAddressAsync(
        ICallerContext caller, EmailAddress emailAddress, CancellationToken cancellationToken)
    {
        var retrievedProfile =
            await _userProfilesService.FindPersonByEmailAddressPrivateAsync(caller, emailAddress,
                cancellationToken);
        if (retrievedProfile.IsFailure)
        {
            return retrievedProfile.Error;
        }

        if (retrievedProfile.Value.HasValue)
        {
            var profile = retrievedProfile.Value.Value;
            var user = await _endUserRepository.LoadAsync(profile.UserId.ToId(), cancellationToken);
            if (user.IsFailure)
            {
                return user.Error;
            }

            return new EndUserWithProfile(user.Value, profile).ToOptional();
        }

        return Optional<EndUserWithProfile>.None;
    }

    private async Task<Result<Optional<EndUserWithProfile>, Error>> FindInvitedGuestWithEmailAddressAsync(
        EmailAddress emailAddress, CancellationToken cancellationToken)
    {
        var invitedGuest =
            await _invitationRepository.FindInvitedGuestByEmailAddressAsync(emailAddress, cancellationToken);
        if (invitedGuest.IsFailure)
        {
            return invitedGuest.Error;
        }

        return invitedGuest.Value.HasValue
            ? new EndUserWithProfile(invitedGuest.Value, null).ToOptional()
            : Optional<EndUserWithProfile>.None;
    }

    private async Task<Result<Optional<EndUserRoot>, Error>> FindInvitedGuestWithInvitationTokenAsync(
        string token, CancellationToken cancellationToken)
    {
        var invitedGuest =
            await _invitationRepository.FindInvitedGuestByTokenAsync(token, cancellationToken);
        if (invitedGuest.IsFailure)
        {
            return invitedGuest.Error;
        }

        return invitedGuest.Value;
    }

    private Optional<List<EmailAddress>> GetPermittedOperators()
    {
        return _settings.Platform.GetString(PermittedOperatorsSettingName)
            .Split(PermittedOperatorsDelimiters)
            .Select(email =>
            {
                var username = EmailAddress.Create(email.Trim());
                if (username.IsFailure)
                {
                    return null;
                }

                return username.Value;
            })
            .Where(username => username is not null)
            .ToList()!;
    }

    private record EndUserWithProfile(EndUserRoot User, UserProfile? Profile);
}

internal static class EndUserConversionExtensions
{
    public static MembershipWithUserProfile ToMembership(this MembershipJoinInvitation membership)
    {
        var dto = new MembershipWithUserProfile
        {
            Id = membership.Id.Value,
            UserId = membership.UserId.Value,
            IsDefault = membership.IsDefault,
            OrganizationId = membership.UserId.Value,
            Ownership = membership.Ownership.Value.ToEnumOrDefault(OrganizationOwnership.Shared),
            Status = membership.Status.Value.ToEnumOrDefault(EndUserStatus.Unregistered),
            Roles = membership.Roles.Value.Denormalize(),
            Features = membership.Features.Value.Denormalize(),
            Profile = null!
        };

        return dto;
    }

    public static Membership ToMembership(this EndUsersDomain.Membership membership)
    {
        return new Membership
        {
            Id = membership.Id,
            UserId = membership.RootId.Value,
            IsDefault = membership.IsDefault,
            OrganizationId = membership.OrganizationId.Value,
            Ownership = membership.Ownership.Value.ToEnumOrDefault(OrganizationOwnership.Shared),
            Features = membership.Features.Denormalize(),
            Roles = membership.Roles.Denormalize()
        };
    }

    public static UserProfile ToUnregisteredUserProfile(this MembershipJoinInvitation membership)
    {
        var dto = new UserProfile
        {
            Id = membership.UserId.Value,
            UserId = membership.UserId.Value,
            EmailAddress = membership.InvitedEmailAddress.Value,
            DisplayName = membership.InvitedEmailAddress.Value,
            Name = new PersonName
            {
                FirstName = membership.InvitedEmailAddress.Value,
                LastName = null
            },
            Classification = UserProfileClassification.Person
        };

        return dto;
    }

    public static EndUser ToUser(this EndUserRoot user)
    {
        return new EndUser
        {
            Id = user.Id,
            Access = user.Access.ToEnumOrDefault(EndUserAccess.Enabled),
            Status = user.Status.ToEnumOrDefault(EndUserStatus.Unregistered),
            Classification = user.Classification.ToEnumOrDefault(EndUserClassification.Person),
            Features = user.Features.Denormalize(),
            Roles = user.Roles.Denormalize()
        };
    }

    public static EndUserWithMemberships ToUserWithMemberships(this EndUserRoot user)
    {
        var endUser = ToUser(user);
        var withMemberships = endUser.Convert<EndUser, EndUserWithMemberships>();
        withMemberships.Memberships = user.Memberships.Select(ms => ms.ToMembership()).ToList();

        return withMemberships;
    }
}