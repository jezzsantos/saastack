using Application.Common;
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
    private readonly INotificationsService _notificationsService;
    private readonly IRecorder _recorder;
    private readonly IConfigurationSettings _settings;
    private readonly IUserProfilesService _userProfilesService;

    public EndUsersApplication(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        INotificationsService notificationsService, IUserProfilesService userProfilesService,
        IInvitationRepository invitationRepository,
        IEndUserRepository endUserRepository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _settings = settings;
        _notificationsService = notificationsService;
        _userProfilesService = userProfilesService;
        _invitationRepository = invitationRepository;
        _endUserRepository = endUserRepository;
    }

    public async Task<Result<EndUser, Error>> GetUserAsync(ICallerContext context, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(context.ToCall(), "Retrieved user: {Id}", user.Id);

        return user.ToUser();
    }

    public async Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller, string organizationId, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var retrieved =
            await _endUserRepository.SearchAllMembershipsByOrganizationAsync(organizationId.ToId(), searchOptions,
                cancellationToken);
        if (!retrieved.IsSuccessful)
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

    public async Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext context, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(context.ToCall(), "Retrieved user with  memberships: {Id}", user.Id);

        return user.ToUserWithMemberships();
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterMachineAsync(ICallerContext context, string name,
        string? timezone, string? countryCode, CancellationToken cancellationToken)
    {
        var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Machine);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var userProfile = EndUserProfile.Create(name, timezone: timezone, countryCode: countryCode);
        if (!userProfile.IsSuccessful)
        {
            return userProfile.Error;
        }

        var machine = created.Value;
        var (platformRoles, platformFeatures, _, _) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.CreatingMachine, context.IsAuthenticated);
        var registered =
            machine.Register(platformRoles, platformFeatures, userProfile.Value, Optional<EmailAddress>.None);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(machine, true, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        machine = saved.Value;
        _recorder.TraceInformation(context.ToCall(), "Registered machine: {Id}", machine.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.MachineRegistered);

        if (context.IsAuthenticated)
        {
            var retrievedAdder = await _endUserRepository.LoadAsync(context.ToCallerId(), cancellationToken);
            if (!retrievedAdder.IsSuccessful)
            {
                return retrievedAdder.Error;
            }

            var adder = retrievedAdder.Value;
            var adderDefaultMembership = adder.DefaultMembership;
            if (adderDefaultMembership.IsShared)
            {
                var (_, _, tenantRoles2, tenantFeatures2) =
                    EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.InvitingMachineToCreatorOrg,
                        context.IsAuthenticated);
                var adderEnrolled = machine.AddMembership(adder, adderDefaultMembership.Ownership,
                    adderDefaultMembership.OrganizationId, tenantRoles2, tenantFeatures2);
                if (!adderEnrolled.IsSuccessful)
                {
                    return adderEnrolled.Error;
                }

                saved = await _endUserRepository.SaveAsync(saved.Value, cancellationToken);
                if (!saved.IsSuccessful)
                {
                    return saved.Error;
                }

                machine = saved.Value;
                _recorder.TraceInformation(context.ToCall(),
                    "Machine {Id} has become a member of {User} organization {Organization}",
                    machine.Id, adder.Id, adderDefaultMembership.OrganizationId);
            }
        }

        var defaultOrganizationId = machine.DefaultMembership.OrganizationId;
        var serviceCaller = Caller.CreateAsMaintenance(context.CallId);
        var profile = await _userProfilesService.GetProfilePrivateAsync(serviceCaller, machine.Id, cancellationToken);
        if (!profile.IsSuccessful)
        {
            return profile.Error;
        }

        return machine.ToRegisteredUser(defaultOrganizationId, profile.Value);
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context,
        string? invitationToken, string emailAddress, string firstName, string? lastName, string? timezone,
        string? countryCode, bool termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        if (!termsAndConditionsAccepted)
        {
            return Error.RuleViolation(Resources.EndUsersApplication_NotAcceptedTerms);
        }

        var email = EmailAddress.Create(emailAddress);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var username = email.Value;

        var existingUser = Optional<EndUserWithProfile>.None;
        if (invitationToken.HasValue())
        {
            var retrievedGuest =
                await FindInvitedGuestWithInvitationTokenAsync(invitationToken, cancellationToken);
            if (!retrievedGuest.IsSuccessful)
            {
                return retrievedGuest.Error;
            }

            if (retrievedGuest.Value.HasValue)
            {
                var existingRegisteredUser =
                    await FindProfileWithEmailAddressAsync(context, username, cancellationToken);
                if (!existingRegisteredUser.IsSuccessful)
                {
                    return existingRegisteredUser.Error;
                }

                if (existingRegisteredUser.Value.HasValue)
                {
                    return Error.EntityExists(Resources.EndUsersApplication_AcceptedInvitationWithExistingEmailAddress);
                }

                var invitee = retrievedGuest.Value.Value;
                var acceptedById = context.ToCallerId();
                var accepted = invitee.AcceptGuestInvitation(acceptedById, username);
                if (!accepted.IsSuccessful)
                {
                    return accepted.Error;
                }

                _recorder.TraceInformation(context.ToCall(), "Guest user {Id} accepted their invitation", invitee.Id);
                existingUser = new EndUserWithProfile(invitee, null);
            }
        }

        if (!existingUser.HasValue)
        {
            var registeredOrGuest =
                await FindRegisteredPersonOrInvitedGuestByEmailAddressAsync(context, username, cancellationToken);
            if (!registeredOrGuest.IsSuccessful)
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

                var notified = await _notificationsService.NotifyReRegistrationCourtesyAsync(context,
                    unregisteredUser.Id, unregisteredUserProfile.EmailAddress, unregisteredUserProfile.DisplayName,
                    unregisteredUserProfile.Timezone, unregisteredUserProfile.Address.CountryCode, cancellationToken);
                if (!notified.IsSuccessful)
                {
                    return notified.Error;
                }

                _recorder.TraceInformation(context.ToCall(),
                    "Attempted re-registration of user: {Id}, with email {EmailAddress}", unregisteredUser.Id, email);
                _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.PersonReRegistered,
                    new Dictionary<string, object>
                    {
                        { UsageConstants.Properties.Id, unregisteredUser.Id },
                        { UsageConstants.Properties.EmailAddress, email }
                    });

                return unregisteredUser.ToRegisteredUser(unregisteredUser.DefaultMembership.Id,
                    unregisteredUserProfile);
            }
        }
        else
        {
            var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            unregisteredUser = created.Value;
        }

        var userProfile = EndUserProfile.Create(firstName, lastName, timezone, countryCode);
        if (!userProfile.IsSuccessful)
        {
            return userProfile.Error;
        }

        var permittedOperators = GetPermittedOperators();
        var (platformRoles, platformFeatures, _, _) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.CreatingPerson, context.IsAuthenticated,
                username, permittedOperators);
        var registered = unregisteredUser.Register(platformRoles, platformFeatures, userProfile.Value, username);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(unregisteredUser, true, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        var person = saved.Value;
        _recorder.TraceInformation(context.ToCall(), "Registered user: {Id}", person.Id);
        _recorder.AuditAgainst(context.ToCall(), person.Id,
            Audits.EndUsersApplication_User_Registered_TermsAccepted,
            "EndUser {Id} accepted their terms and conditions", person.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.PersonRegistrationCreated);

        var defaultOrganizationId = person.DefaultMembership.OrganizationId;
        var serviceCaller = Caller.CreateAsMaintenance(context.CallId);
        var profile = await _userProfilesService.GetProfilePrivateAsync(serviceCaller, person.Id, cancellationToken);
        if (!profile.IsSuccessful)
        {
            return profile.Error;
        }

        return person.ToRegisteredUser(defaultOrganizationId, profile.Value);
    }

    public async Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAddressAsync(ICallerContext context,
        string emailAddress, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(emailAddress);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var retrieved =
            await FindRegisteredPersonOrInvitedGuestByEmailAddressAsync(context, email.Value, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        return retrieved.Value.HasValue
            ? retrieved.Value.Value.User.ToUser().ToOptional()
            : Optional<EndUser>.None;
    }

    public async Task<Result<EndUser, Error>> AssignPlatformRolesAsync(ICallerContext context, string id,
        List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrievedAssignee.IsSuccessful)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _endUserRepository.LoadAsync(context.ToCallerId(), cancellationToken);
        if (!retrievedAssigner.IsSuccessful)
        {
            return retrievedAssigner.Error;
        }

        var assignee = retrievedAssignee.Value;
        var assigner = retrievedAssigner.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (!assigneeRoles.IsSuccessful)
        {
            return assigneeRoles.Error;
        }

        var assigned = assignee.AssignPlatformRoles(assigner, assigneeRoles.Value);
        if (!assigned.IsSuccessful)
        {
            return assigned.Error;
        }

        var updated = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(),
            "EndUser {Id} has been assigned platform roles {Roles}",
            assignee.Id, roles.JoinAsOredChoices());
        _recorder.AuditAgainst(context.ToCall(), assignee.Id,
            Audits.EndUserApplication_PlatformRolesAssigned,
            "EndUser {AssignerId} assigned the platform roles {Roles} to assignee {AssigneeId}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id);

        return updated.Value.ToUser();
    }

    public async Task<Result<EndUser, Error>> UnassignPlatformRolesAsync(ICallerContext context, string id,
        List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrievedAssignee.IsSuccessful)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _endUserRepository.LoadAsync(context.ToCallerId(), cancellationToken);
        if (!retrievedAssigner.IsSuccessful)
        {
            return retrievedAssigner.Error;
        }

        var assignee = retrievedAssignee.Value;
        var assigner = retrievedAssigner.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (!assigneeRoles.IsSuccessful)
        {
            return assigneeRoles.Error;
        }

        var unassigned = assignee.UnassignPlatformRoles(assigner, assigneeRoles.Value);
        if (!unassigned.IsSuccessful)
        {
            return unassigned.Error;
        }

        var updated = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(),
            "EndUser {Id} has been unassigned platform roles {Roles}",
            assignee.Id, roles.JoinAsOredChoices());
        _recorder.AuditAgainst(context.ToCall(), assignee.Id,
            Audits.EndUserApplication_PlatformRolesUnassigned,
            "EndUser {AssignerId} unassigned the platform roles {Roles} from assignee {AssigneeId}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id);

        return updated.Value.ToUser();
    }

    public async Task<Result<EndUserWithMemberships, Error>> AssignTenantRolesAsync(ICallerContext context,
        string organizationId,
        string id, List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _endUserRepository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrievedAssignee.IsSuccessful)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _endUserRepository.LoadAsync(context.ToCallerId(), cancellationToken);
        if (!retrievedAssigner.IsSuccessful)
        {
            return retrievedAssigner.Error;
        }

        var assignee = retrievedAssignee.Value;
        var assigner = retrievedAssigner.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (!assigneeRoles.IsSuccessful)
        {
            return assigneeRoles.Error;
        }

        var assigned = assignee.AssignMembershipRoles(assigner, organizationId.ToId(), assigneeRoles.Value);
        if (!assigned.IsSuccessful)
        {
            return assigned.Error;
        }

        var membership = assigned.Value;
        var updated = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        _recorder.TraceInformation(context.ToCall(),
            "EndUser {Id} has been assigned tenant roles {Roles} to membership {Membership}",
            assignee.Id, roles.JoinAsOredChoices(), membership.Id);
        _recorder.AuditAgainst(context.ToCall(), assignee.Id,
            Audits.EndUserApplication_TenantRolesAssigned,
            "EndUser {AssignerId} assigned the tenant roles {Roles} to assignee {AssigneeId} for membership {Membership}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id, membership.Id);

        return assignee.ToUserWithMemberships();
    }

    private async Task<Result<Membership, Error>> CreateMembershipAsync(ICallerContext context,
        Identifier createdById, Identifier organizationId, OrganizationOwnership ownership,
        CancellationToken cancellationToken)
    {
        var retrievedInviter = await _endUserRepository.LoadAsync(createdById, cancellationToken);
        if (!retrievedInviter.IsSuccessful)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;
        var useCase = ownership switch
        {
            OrganizationOwnership.Shared => RolesAndFeaturesUseCase.CreatingOrg,
            OrganizationOwnership.Personal => inviter.Classification == UserClassification.Person
                ? RolesAndFeaturesUseCase.CreatingPerson
                : RolesAndFeaturesUseCase.CreatingMachine,
            _ => RolesAndFeaturesUseCase.CreatingOrg
        };
        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(useCase, context.IsAuthenticated);
        var inviterOwnership = ownership.ToEnumOrDefault(Domain.Shared.Organizations.OrganizationOwnership.Shared);
        var membered = inviter.AddMembership(inviter, inviterOwnership, organizationId, tenantRoles, tenantFeatures);
        if (!membered.IsSuccessful)
        {
            return membered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(inviter, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "EndUser {Id} has become a member of organization {Organization}",
            inviter.Id, organizationId);

        var membership = saved.Value.FindMembership(organizationId);
        if (!membership.HasValue)
        {
            return Error.EntityNotFound(Resources.EndUsersApplication_MembershipNotFound);
        }

        return membership.Value.ToMembership();
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
            var member = membership.ToMembership();
            member.Profile = membership.Status.Value.ToEnumOrDefault(EndUserStatus.Unregistered)
                             == EndUserStatus.Unregistered
                ? membership.ToUnregisteredUserProfile()
                : profiles.First(profile => profile.UserId == membership.UserId);

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
        if (!existingProfile.IsSuccessful)
        {
            return existingProfile.Error;
        }

        if (existingProfile.Value.HasValue)
        {
            return existingProfile;
        }

        var existingInvitation = await FindInvitedGuestWithEmailAddressAsync(emailAddress, cancellationToken);
        if (!existingInvitation.IsSuccessful)
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
        if (!retrievedProfile.IsSuccessful)
        {
            return retrievedProfile.Error;
        }

        if (retrievedProfile.Value.HasValue)
        {
            var profile = retrievedProfile.Value.Value;
            var user = await _endUserRepository.LoadAsync(profile.UserId.ToId(), cancellationToken);
            if (!user.IsSuccessful)
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
        if (!invitedGuest.IsSuccessful)
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
        if (!invitedGuest.IsSuccessful)
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
                if (!username.IsSuccessful)
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
            Status = membership.Status.Value.ToEnumOrDefault(EndUserStatus.Unregistered),
            Roles = membership.Roles.Value.ToList(),
            Features = membership.Features.Value.ToList(),
            Profile = null!
        };

        return dto;
    }

    public static Membership ToMembership(this EndUsersDomain.Membership ms)
    {
        return new Membership
        {
            Id = ms.Id,
            UserId = ms.RootId.Value,
            IsDefault = ms.IsDefault,
            OrganizationId = ms.OrganizationId.Value,
            Features = ms.Features.ToList(),
            Roles = ms.Roles.ToList()
        };
    }

    public static RegisteredEndUser ToRegisteredUser(this EndUserRoot user, Identifier defaultOrganizationId,
        UserProfile profile)
    {
        var endUser = ToUser(user);
        var registeredUser = endUser.Convert<EndUser, RegisteredEndUser>();
        registeredUser.Profile = profile.Convert<UserProfile, UserProfileWithDefaultMembership>();
        registeredUser.Profile.DefaultOrganizationId = defaultOrganizationId;

        return registeredUser;
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
            Features = user.Features.ToList(),
            Roles = user.Roles.ToList()
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