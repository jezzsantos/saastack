using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.EndUsers;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace OrganizationsApplication;

public partial class OrganizationsApplication : IOrganizationsApplication
{
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IImagesService _imagesService;
    private readonly IRecorder _recorder;
    private readonly IOrganizationRepository _repository;
    private readonly ITenantSettingService _tenantSettingService;
    private readonly ITenantSettingsService _tenantSettingsService;

    public OrganizationsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITenantSettingsService tenantSettingsService, ITenantSettingService tenantSettingService,
        IEndUsersService endUsersService, IImagesService imagesService,
        IOrganizationRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tenantSettingService = tenantSettingService;
        _endUsersService = endUsersService;
        _imagesService = imagesService;
        _tenantSettingsService = tenantSettingsService;
        _repository = repository;
    }

    public async Task<Result<Organization, Error>> ChangeDetailsAsync(ICallerContext caller, string id,
        string? name, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        if (name.HasValue())
        {
            var modifierRoles = Roles.Create(caller.Roles.Tenant);
            if (!modifierRoles.IsSuccessful)
            {
                return modifierRoles.Error;
            }

            var orgName = DisplayName.Create(name);
            if (!orgName.IsSuccessful)
            {
                return orgName.Error;
            }

            var changed = org.ChangeName(caller.ToCallerId(), modifierRoles.Value, orgName.Value);
            if (!changed.IsSuccessful)
            {
                return changed.Error;
            }
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Changed organization: {Id}", org.Id);

        return org.ToOrganization();
    }

    public async Task<Result<Error>> ChangeSettingsAsync(ICallerContext caller, string id,
        TenantSettings settings, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var newSettings = settings.ToSettings();
        if (!newSettings.IsSuccessful)
        {
            return newSettings.Error;
        }

        var updated = org.UpdateSettings(newSettings.Value);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Updated the settings of organization: {Id}", org.Id);

        return Result.Ok;
    }

    public async Task<Result<Organization, Error>> CreateSharedOrganizationAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken)
    {
        var userId = caller.ToCallerId();
        var retrieved = await _endUsersService.GetUserPrivateAsync(caller, userId, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;
        var created = await CreateOrganizationInternalAsync(caller, user.Id,
            user.Classification.ToEnumOrDefault(UserClassification.Person), name,
            OrganizationOwnership.Shared, cancellationToken);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        return created.Value;
    }

    public async Task<Result<Error>> DeleteOrganizationAsync(ICallerContext caller, string? id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var deleterRoles = Roles.Create(caller.Roles.Tenant);
        if (!deleterRoles.IsSuccessful)
        {
            return deleterRoles.Error;
        }

        var org = retrieved.Value;
        var deleterId = caller.ToCallerId();
        var deleted = org.DeleteOrganization(deleterId, deleterRoles.Value);
        if (!deleted.IsSuccessful)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Deleted organization: {Id}", org.Id);

        return Result.Ok;
    }

    public async Task<Result<Organization, Error>> GetOrganizationAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;

        _recorder.TraceInformation(caller.ToCall(), "Retrieved organization: {Id}", org.Id);

        return org.ToOrganization();
    }

#if TESTINGONLY
    public async Task<Result<OrganizationWithSettings, Error>> GetOrganizationSettingsAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;

        _recorder.TraceInformation(caller.ToCall(), "Retrieved organization: {Id}", org.Id);

        return org.ToOrganizationWithSettings();
    }
#endif

    public async Task<Result<TenantSettings, Error>> GetSettingsAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var settings = org.Settings;

        _recorder.TraceInformation(caller.ToCall(), "Retrieved organization: {Id} settings", org.Id);

        return settings.ToSettings();
    }

    public async Task<Result<Organization, Error>> InviteMemberToOrganizationAsync(ICallerContext caller, string id,
        string? userId, string? emailAddress, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var inviterRoles = Roles.Create(caller.Roles.Tenant);
        if (!inviterRoles.IsSuccessful)
        {
            return inviterRoles.Error;
        }

        if (emailAddress.HasValue())
        {
            var email = EmailAddress.Create(emailAddress);
            if (!email.IsSuccessful)
            {
                return email.Error;
            }

            var invited = org.InviteMember(caller.ToCallerId(), inviterRoles.Value, Optional<Identifier>.None,
                email.Value);
            if (!invited.IsSuccessful)
            {
                return invited.Error;
            }

            var saved = await _repository.SaveAsync(org, cancellationToken);
            if (!saved.IsSuccessful)
            {
                return saved.Error;
            }

            org = saved.Value;
            _recorder.TraceInformation(caller.ToCall(), "Organization {Id} has invited {UserEmail} to be a member",
                org.Id, emailAddress);

            return org.ToOrganization();
        }

        if (userId.HasValue())
        {
            var invited = org.InviteMember(caller.ToCallerId(), inviterRoles.Value, userId.ToId(),
                Optional<EmailAddress>.None);
            if (!invited.IsSuccessful)
            {
                return invited.Error;
            }

            var saved = await _repository.SaveAsync(org, cancellationToken);
            if (!saved.IsSuccessful)
            {
                return saved.Error;
            }

            org = saved.Value;
            _recorder.TraceInformation(caller.ToCall(), "Organization {Id} has invited {UserId} to be a member",
                org.Id, userId);

            return org.ToOrganization();
        }

        return Error.RuleViolation(Resources.OrganizationApplication_InvitedNoUserNorEmail);
    }

    public async Task<Result<Organization, Error>> UnInviteMemberFromOrganizationAsync(ICallerContext caller, string id,
        string userId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var removerRoles = Roles.Create(caller.Roles.Tenant);
        if (!removerRoles.IsSuccessful)
        {
            return removerRoles.Error;
        }

        var org = retrieved.Value;
        var uninvited = org.UnInviteMember(caller.ToCallerId(), removerRoles.Value, userId.ToId());
        if (!uninvited.IsSuccessful)
        {
            return uninvited.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Uninvited member {UserId} from organization: {Id}", userId,
            org.Id);

        return org.ToOrganization();
    }

    public async Task<Result<SearchResults<OrganizationMember>, Error>> ListMembersForOrganizationAsync(
        ICallerContext caller, string? id, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var memberships =
            await _endUsersService.ListMembershipsForOrganizationAsync(caller, org.Id, searchOptions,
                getOptions, cancellationToken);
        if (!memberships.IsSuccessful)
        {
            return memberships.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} listed its members", org.Id);

        return searchOptions.ApplyWithMetadata(memberships.Value.Results.ConvertAll(x => x.ToMember()));
    }

    public async Task<Result<Organization, Error>> AssignRolesToOrganizationAsync(ICallerContext caller, string id,
        string userId, List<string> roles, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var assignerRoles = Roles.Create(caller.Roles.Tenant);
        if (!assignerRoles.IsSuccessful)
        {
            return assignerRoles.Error;
        }

        var rolesToAssign = Roles.Create(roles.ToArray());
        if (!rolesToAssign.IsSuccessful)
        {
            return rolesToAssign.Error;
        }

        var org = retrieved.Value;
        var assignerId = caller.ToCallerId();
        var assigned = org.AssignRoles(assignerId, assignerRoles.Value, userId.ToId(), rolesToAssign.Value);
        if (!assigned.IsSuccessful)
        {
            return assigned.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} assigned roles for {User}", org.Id, userId);

        return org.ToOrganization();
    }

    public async Task<Result<Organization, Error>> UnassignRolesFromOrganizationAsync(ICallerContext caller, string id,
        string userId, List<string> roles, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var assignerRoles = Roles.Create(caller.Roles.Tenant);
        if (!assignerRoles.IsSuccessful)
        {
            return assignerRoles.Error;
        }

        var rolesToUnassign = Roles.Create(roles.ToArray());
        if (!rolesToUnassign.IsSuccessful)
        {
            return rolesToUnassign.Error;
        }

        var org = retrieved.Value;
        var assignerId = caller.ToCallerId();
        var unassigned = org.UnassignRoles(assignerId, assignerRoles.Value, userId.ToId(), rolesToUnassign.Value);
        if (!unassigned.IsSuccessful)
        {
            return unassigned.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} unassigned roles for {User}", org.Id, userId);

        return org.ToOrganization();
    }

    public async Task<Result<Organization, Error>> ChangeAvatarAsync(ICallerContext caller, string id,
        FileUpload upload, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var modifierRoles = Roles.Create(caller.Roles.Tenant);
        if (!modifierRoles.IsSuccessful)
        {
            return modifierRoles.Error;
        }

        var org = retrieved.Value;
        var avatared = await ChangeAvatarInternalAsync(caller, caller.ToCallerId(), modifierRoles.Value, org, upload,
            cancellationToken);
        if (!avatared.IsSuccessful)
        {
            return avatared.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Changed avatar for organization: {Id}", org.Id);

        return org.ToOrganization();
    }

    public async Task<Result<Organization, Error>> DeleteAvatarAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var deleterRoles = Roles.Create(caller.Roles.Tenant);
        if (!deleterRoles.IsSuccessful)
        {
            return deleterRoles.Error;
        }

        var org = retrieved.Value;
        var deleted = await org.DeleteAvatarAsync(caller.ToCallerId(), deleterRoles.Value, async avatarId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, avatarId, cancellationToken);
            return !removed.IsSuccessful
                ? removed.Error
                : Result.Ok;
        });
        if (!deleted.IsSuccessful)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} avatar was deleted", org.Id);

        return org.ToOrganization();
    }

    private async Task<Result<Organization, Error>> CreateOrganizationInternalAsync(ICallerContext caller,
        string creatorId, UserClassification classification, string name, OrganizationOwnership ownership,
        CancellationToken cancellationToken)
    {
        var displayName = DisplayName.Create(name);
        if (!displayName.IsSuccessful)
        {
            return displayName.Error;
        }

        var created = OrganizationRoot.Create(_recorder, _identifierFactory, _tenantSettingService,
            ownership, creatorId.ToId(), classification, displayName.Value);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var org = created.Value;
        var newSettings = await _tenantSettingsService.CreateForTenantAsync(caller, org.Id, cancellationToken);
        if (!newSettings.IsSuccessful)
        {
            return newSettings.Error;
        }

        var settings = newSettings.Value.ToSettings();
        if (!settings.IsSuccessful)
        {
            return settings.Error;
        }

        var configured = org.CreateSettings(settings.Value);
        if (!configured.IsSuccessful)
        {
            return configured.Error;
        }

        //TODO: Get the billing details for the creator and add the billing subscription for them

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Created organization: {Id}, by {CreatedBy}", org.Id,
            saved.Value.CreatedById);

        return saved.Value.ToOrganization();
    }

    private async Task<Result<Error>> ChangeAvatarInternalAsync(ICallerContext caller, Identifier modifierId,
        Roles modifierRoles,
        OrganizationRoot organization, FileUpload upload, CancellationToken cancellationToken)
    {
        return await organization.ChangeAvatarAsync(modifierId, modifierRoles, async name =>
        {
            var created = await _imagesService.CreateImageAsync(caller, upload, name.Text, cancellationToken);
            if (!created.IsSuccessful)
            {
                return created.Error;
            }

            return Avatar.Create(created.Value.Id.ToId(), created.Value.Url);
        }, async avatarId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, avatarId, cancellationToken);
            return !removed.IsSuccessful
                ? removed.Error
                : Result.Ok;
        });
    }
}

internal static class OrganizationConversionExtensions
{
    public static OrganizationMember ToMember(this MembershipWithUserProfile membership)
    {
        var dto = new OrganizationMember
        {
            Id = membership.Id,
            UserId = membership.UserId,
            IsDefault = membership.IsDefault,
            IsRegistered = membership.Status == EndUserStatus.Registered,
            IsOwner = membership.Roles.Contains(TenantRoles.Owner.Name),
            Roles = membership.Roles,
            Features = membership.Features,
            EmailAddress = membership.Profile.EmailAddress,
            Name = membership.Profile.Name,
            Classification = membership.Profile.Classification
        };

        return dto;
    }

    public static Organization ToOrganization(this OrganizationRoot organization)
    {
        return new Organization
        {
            Id = organization.Id,
            Name = organization.Name,
            CreatedById = organization.CreatedById,
            Ownership = organization.Ownership.ToEnumOrDefault(
                Application.Resources.Shared.OrganizationOwnership.Shared),
            AvatarUrl = organization.Avatar.HasValue
                ? organization.Avatar.Value.Url
                : null
        };
    }

    public static OrganizationWithSettings ToOrganizationWithSettings(this OrganizationRoot organization)
    {
        var dto = organization.ToOrganization().Convert<Organization, OrganizationWithSettings>();
        dto.Settings =
            organization.Settings.Properties.ToDictionary(pair => pair.Key,
                pair => pair.Value.Value.ToString() ?? string.Empty);
        return dto;
    }

    public static Result<Settings, Error> ToSettings(this TenantSettings tenantSettings)
    {
        var settings = Settings.Empty;
        foreach (var (key, tenantSetting) in tenantSettings)
        {
            if (tenantSetting.Value.NotExists())
            {
                continue;
            }

            var value = tenantSetting.Value;
            var setting = Setting.Create(value, tenantSetting.IsEncrypted);
            if (!setting.IsSuccessful)
            {
                return setting.Error;
            }

            var added = settings.AddOrUpdate(key, setting.Value);
            if (!added.IsSuccessful)
            {
                return added.Error;
            }

            settings = added.Value;
        }

        return settings;
    }

    public static TenantSettings ToSettings(this Settings settings)
    {
        var dictionary = settings.Properties.ToDictionary(pair => pair.Key, pair => new TenantSetting
        {
            Value = pair.Value.Value,
            IsEncrypted = pair.Value.IsEncrypted
        });

        return new TenantSettings(dictionary);
    }
}