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
using EndUsersApplication.Persistence;
using EndUsersDomain;
using Membership = Application.Resources.Shared.Membership;
using PersonName = Domain.Shared.PersonName;

namespace EndUsersApplication;

public class EndUsersApplication : IEndUsersApplication
{
    internal const string PermittedOperatorsSettingName = "Hosts:EndUsersApi:Authorization:OperatorWhitelist";
    private static readonly char[] PermittedOperatorsDelimiters = [';', ',', ' '];
    private readonly IIdentifierFactory _idFactory;
    private readonly INotificationsService _notificationsService;
    private readonly IOrganizationsService _organizationsService;
    private readonly IRecorder _recorder;
    private readonly IEndUserRepository _repository;
    private readonly IConfigurationSettings _settings;
    private readonly IUserProfilesService _userProfilesService;

    public EndUsersApplication(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        INotificationsService notificationsService, IOrganizationsService organizationsService,
        IUserProfilesService userProfilesService,
        IEndUserRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _settings = settings;
        _notificationsService = notificationsService;
        _organizationsService = organizationsService;
        _userProfilesService = userProfilesService;
        _repository = repository;
    }

    public async Task<Result<EndUser, Error>> GetPersonAsync(ICallerContext context, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(context.ToCall(), "Retrieved user: {Id}", user.Id);

        return user.ToUser();
    }

    public async Task<Result<EndUserWithMemberships, Error>> GetMembershipsAsync(ICallerContext context, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
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

        var machine = created.Value;
        var profiled = await _userProfilesService.CreateMachineProfilePrivateAsync(context, machine.Id, name, timezone,
            countryCode, cancellationToken);
        if (!profiled.IsSuccessful)
        {
            return profiled.Error;
        }

        var profile = profiled.Value;
        var (platformRoles, platformFeatures, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(UserClassification.Machine, context.IsAuthenticated,
                Optional<EmailAddress>.None, Optional<List<EmailAddress>>.None);
        var registered = machine.Register(platformRoles, platformFeatures, Optional<EmailAddress>.None);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var defaultOrganization =
            await _organizationsService.CreateOrganizationPrivateAsync(context, machine.Id, name,
                OrganizationOwnership.Personal, cancellationToken);
        if (!defaultOrganization.IsSuccessful)
        {
            return defaultOrganization.Error;
        }

        var defaultOrganizationId = defaultOrganization.Value.Id.ToId();
        var selfEnrolled = machine.AddMembership(defaultOrganizationId, tenantRoles,
            tenantFeatures);
        if (!selfEnrolled.IsSuccessful)
        {
            return selfEnrolled.Error;
        }

        if (context.IsAuthenticated)
        {
            var adder = await _repository.LoadAsync(context.ToCallerId(), cancellationToken);
            if (!adder.IsSuccessful)
            {
                return adder.Error;
            }

            var adderDefaultOrganizationId = adder.Value.Memberships.DefaultMembership.OrganizationId;
            var adderEnrolled = machine.AddMembership(adderDefaultOrganizationId, tenantRoles,
                tenantFeatures);
            if (!adderEnrolled.IsSuccessful)
            {
                return adderEnrolled.Error;
            }
        }

        var saved = await _repository.SaveAsync(machine, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Registered machine: {Id}", machine.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.MachineRegistered);

        return machine.ToRegisteredUser(defaultOrganizationId, profile);
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context, string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        if (!termsAndConditionsAccepted)
        {
            return Error.RuleViolation(Resources.EndUsersApplication_NotAcceptedTerms);
        }

        var username = EmailAddress.Create(emailAddress);
        if (!username.IsSuccessful)
        {
            return username.Error;
        }

        var existingUser = await FindPersonByEmailAddressInternalAsync(context, username.Value, cancellationToken);
        if (!existingUser.IsSuccessful)
        {
            return existingUser.Error;
        }

        EndUserRoot user;
        UserProfile? profile;
        if (existingUser.Value.HasValue)
        {
            user = existingUser.Value.Value.User;
            if (user.Status == UserStatus.Registered)
            {
                profile = existingUser.Value.Value.Profile;
                if (profile.NotExists()
                    || profile.Type != UserProfileType.Person
                    || profile.EmailAddress.HasNoValue())
                {
                    return Error.EntityNotFound(Resources.EndUsersApplication_NotPersonProfile);
                }

                var notified = await _notificationsService.NotifyReRegistrationCourtesyAsync(context, user.Id,
                    profile.EmailAddress, profile.DisplayName, profile.Timezone, profile.Address.CountryCode,
                    cancellationToken);
                if (!notified.IsSuccessful)
                {
                    return notified.Error;
                }

                _recorder.TraceInformation(context.ToCall(),
                    "Attempted re-registration of user: {Id}, with email {EmailAddress}", user.Id, emailAddress);
                _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.PersonReRegistered,
                    new Dictionary<string, object>
                    {
                        { UsageConstants.Properties.Id, user.Id },
                        { UsageConstants.Properties.EmailAddress, emailAddress }
                    });

                return user.ToRegisteredUser(user.Memberships.DefaultMembership.Id, profile);
            }
        }
        else
        {
            var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            user = created.Value;
        }

        var profiled = await _userProfilesService.CreatePersonProfilePrivateAsync(context, user.Id, emailAddress,
            firstName, lastName, timezone, countryCode, cancellationToken);
        if (!profiled.IsSuccessful)
        {
            return profiled.Error;
        }

        profile = profiled.Value;
        var permittedOperators = GetPermittedOperators();
        var (platformRoles, platformFeatures, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(UserClassification.Person, context.IsAuthenticated, username.Value,
                permittedOperators);
        var registered = user.Register(platformRoles, platformFeatures, username.Value);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var organizationName = PersonName.Create(firstName, lastName);
        if (!organizationName.IsSuccessful)
        {
            return organizationName.Error;
        }

        var defaultOrganization =
            await _organizationsService.CreateOrganizationPrivateAsync(context, user.Id,
                organizationName.Value.FullName, OrganizationOwnership.Personal,
                cancellationToken);
        if (!defaultOrganization.IsSuccessful)
        {
            return defaultOrganization.Error;
        }

        var defaultOrganizationId = defaultOrganization.Value.Id.ToId();
        var enrolled = user.AddMembership(defaultOrganizationId, tenantRoles,
            tenantFeatures);
        if (!enrolled.IsSuccessful)
        {
            return enrolled.Error;
        }

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Registered user: {Id}", user.Id);
        _recorder.AuditAgainst(context.ToCall(), user.Id,
            Audits.EndUsersApplication_User_Registered_TermsAccepted,
            "EndUser {Id} accepted their terms and conditions", user.Id);
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.PersonRegistrationCreated);

        return user.ToRegisteredUser(defaultOrganizationId, profile);
    }

    public async Task<Result<Membership, Error>> CreateMembershipForCallerAsync(ICallerContext context,
        string organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(context.ToCallerId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;
        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(UserClassification.Person, context.IsAuthenticated,
                Optional<EmailAddress>.None,
                Optional<List<EmailAddress>>.None);
        var membered = user.AddMembership(organizationId.ToId(), tenantRoles, tenantFeatures);
        if (!membered.IsSuccessful)
        {
            return membered.Error;
        }

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "EndUser {Id} has become a member of organization {Organization}",
            user.Id, organizationId);

        var membership = saved.Value.FindMembership(organizationId.ToId());
        if (!membership.HasValue)
        {
            return Error.EntityNotFound(Resources.EndUsersApplication_MembershipNotFound);
        }

        return membership.Value.ToMembership();
    }

    public async Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAddressAsync(ICallerContext context,
        string emailAddress, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(emailAddress);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var retrieved = await FindPersonByEmailAddressInternalAsync(context, email.Value, cancellationToken);
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
        var retrievedAssignee = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrievedAssignee.IsSuccessful)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _repository.LoadAsync(context.ToCallerId(), cancellationToken);
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

        var updated = await _repository.SaveAsync(assignee, cancellationToken);
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

    public async Task<Result<EndUserWithMemberships, Error>> AssignTenantRolesAsync(ICallerContext context,
        string organizationId,
        string id, List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssignee = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrievedAssignee.IsSuccessful)
        {
            return retrievedAssignee.Error;
        }

        var retrievedAssigner = await _repository.LoadAsync(context.ToCallerId(), cancellationToken);
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
        var updated = await _repository.SaveAsync(assignee, cancellationToken);
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

    private async Task<Result<Optional<EndUserWithProfile>, Error>> FindPersonByEmailAddressInternalAsync(
        ICallerContext caller, EmailAddress emailAddress, CancellationToken cancellationToken)
    {
        var retrieved =
            await _userProfilesService.FindPersonByEmailAddressPrivateAsync(caller, emailAddress, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (retrieved.Value.HasValue)
        {
            var profile = retrieved.Value.Value;
            var user = await _repository.LoadAsync(profile.UserId.ToId(), cancellationToken);
            if (!user.IsSuccessful)
            {
                return user.Error;
            }

            return new EndUserWithProfile(user.Value, profile).ToOptional();
        }

        var invitedGuest = await _repository.FindInvitedGuestByEmailAddressAsync(emailAddress, cancellationToken);
        if (!invitedGuest.IsSuccessful)
        {
            return invitedGuest.Error;
        }

        return invitedGuest.Value.HasValue
            ? new EndUserWithProfile(invitedGuest.Value, null).ToOptional()
            : Optional<EndUserWithProfile>.None;
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
    public static Membership ToMembership(this EndUsersDomain.Membership ms)
    {
        return new Membership
        {
            Id = ms.Id,
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