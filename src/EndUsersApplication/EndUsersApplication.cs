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
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication;

public class EndUsersApplication : IEndUsersApplication
{
    internal const string PermittedOperatorsSettingName = "Hosts:EndUsersApi:Authorization:OperatorWhitelist";
    private static readonly char[] PermittedOperatorsDelimiters = { ';', ',', ' ' };
    private readonly IIdentifierFactory _idFactory;
    private readonly IOrganizationsService _organizationsService;
    private readonly IRecorder _recorder;
    private readonly IEndUserRepository _repository;
    private readonly IConfigurationSettings _settings;

    public EndUsersApplication(IRecorder recorder, IIdentifierFactory idFactory, IConfigurationSettings settings,
        IOrganizationsService organizationsService, IEndUserRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _settings = settings;
        _organizationsService = organizationsService;
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

        //TODO: get/create the profile for this machine (with first,tz,cc) - it may already be existent or not

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

        return machine.ToRegisteredUser(defaultOrganizationId, null, name, null, Timezones.FindOrDefault(timezone),
            CountryCodes.FindOrDefault(countryCode));
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context, string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        if (!termsAndConditionsAccepted)
        {
            return Error.RuleViolation(Resources.EndUsersApplication_NotAcceptedTerms);
        }

        var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var user = created.Value;

        //TODO: get/create the profile for this user (with email, first,last,tz,cc) - it may already be existent or not,
        // if existent, then someone attempted to re-register with the same email address! we need to send a courtesy notification to them.
        // if not, it needs to be created

        var username = EmailAddress.Create(emailAddress);
        if (!username.IsSuccessful)
        {
            return username.Error;
        }

        var permittedOperators = GetPermittedOperators();
        var (platformRoles, platformFeatures, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(UserClassification.Person, context.IsAuthenticated, username.Value,
                permittedOperators);
        var registered = user.Register(platformRoles, platformFeatures, username.Value);
        if (!registered.IsSuccessful)
        {
            return registered.Error;
        }

        var organizationName = Domain.Shared.PersonName.Create(firstName, lastName);
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

        return user.ToRegisteredUser(defaultOrganizationId, emailAddress, firstName, lastName,
            Timezones.FindOrDefault(timezone),
            CountryCodes.FindOrDefault(countryCode));
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

        _recorder.TraceInformation(context.ToCall(), "EndUser {Id} has become a member of organisation {Organisation}",
            user.Id, organizationId);

        var membership = saved.Value.FindMembership(organizationId.ToId());
        if (!membership.HasValue)
        {
            return Error.EntityNotFound(Resources.EndUsersApplication_MembershipNotFound);
        }

        return membership.Value.ToMembership();
    }

    public async Task<Result<Optional<EndUser>, Error>> FindPersonByEmailAsync(ICallerContext context,
        string emailAddress, CancellationToken cancellationToken)
    {
        //TODO: find the profile of this person by email address from the profilesService
        //And then, if not, lookup a endUser that has the email address as an invited guest

        var match = EmailAddress.Create(emailAddress);
        if (!match.IsSuccessful)
        {
            return match.Error;
        }

        var retrieved = await _repository.FindByEmailAddressAsync(match.Value, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (retrieved.Value.HasValue)
        {
            var endUser = retrieved.Value.Value;
            return endUser.ToUser().ToOptional();
        }

        return Optional<EndUser>.None;
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
        string? emailAddress, string firstName,
        string? lastName, TimezoneIANA timezone, CountryCodeIso3166 countryCode)
    {
        var endUser = ToUser(user);
        var registeredUser = endUser.Convert<EndUser, RegisteredEndUser>();
        registeredUser.Profile = new ProfileWithDefaultMembership
        {
            Address = new ProfileAddress
            {
                CountryCode = countryCode.ToString()
            },
            AvatarUrl = null,
            DisplayName = firstName,
            EmailAddress = emailAddress,
            Name = new PersonName { FirstName = firstName, LastName = lastName },
            PhoneNumber = null,
            Timezone = timezone.ToString(),
            Id = user.Id,
            DefaultOrganizationId = defaultOrganizationId
        };

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