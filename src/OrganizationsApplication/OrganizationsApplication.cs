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
using OrganizationsApplication.Persistence;
using OrganizationsDomain;

namespace OrganizationsApplication;

public class OrganizationsApplication : IOrganizationsApplication
{
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly IOrganizationRepository _repository;
    private readonly ITenantSettingService _tenantSettingService;
    private readonly ITenantSettingsService _tenantSettingsService;

    public OrganizationsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITenantSettingsService tenantSettingsService, ITenantSettingService tenantSettingService,
        IEndUsersService endUsersService,
        IOrganizationRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tenantSettingService = tenantSettingService;
        _endUsersService = endUsersService;
        _tenantSettingsService = tenantSettingsService;
        _repository = repository;
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

    public async Task<Result<Organization, Error>> CreateOrganizationAsync(ICallerContext caller, string creatorId,
        string name, OrganizationOwnership ownership, CancellationToken cancellationToken)
    {
        var displayName = DisplayName.Create(name);
        if (!displayName.IsSuccessful)
        {
            return displayName.Error;
        }

        var created = OrganizationRoot.Create(_recorder, _identifierFactory, _tenantSettingService,
            ownership.ToEnumOrDefault(Ownership.Shared), creatorId.ToId(), displayName.Value);
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

        var organizationSettings = newSettings.Value.ToSettings();
        if (!organizationSettings.IsSuccessful)
        {
            return organizationSettings.Error;
        }

        var configured = org.CreateSettings(organizationSettings.Value);
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

    public async Task<Result<Organization, Error>> CreateSharedOrganizationAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken)
    {
        var creatorId = caller.CallerId;
        var created = await CreateOrganizationAsync(caller, creatorId, name, OrganizationOwnership.Shared,
            cancellationToken);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var organization = created.Value;
        var membership =
            await _endUsersService.CreateMembershipForCallerPrivateAsync(caller, organization.Id, cancellationToken);
        if (!membership.IsSuccessful)
        {
            return membership.Error;
        }

        return organization;
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

        var organization = retrieved.Value;
        var inviterRoles = Roles.Create(caller.Roles.Tenant);
        if (!inviterRoles.IsSuccessful)
        {
            return inviterRoles.Error;
        }

        Identifier? addedUserId = null;
        var added = await organization.AddMembershipAsync(caller.ToCallerId(), inviterRoles.Value, async () =>
        {
            var membership =
                await _endUsersService.InviteMemberToOrganizationPrivateAsync(caller, id, userId, emailAddress,
                    cancellationToken);
            if (!membership.IsSuccessful)
            {
                return membership.Error;
            }

            addedUserId = membership.Value.UserId.ToId();
            return Result.Ok;
        });
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} has invited {UserId} to be a member",
            organization.Id, addedUserId!);

        return organization.ToOrganization();
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

        var organization = retrieved.Value;
        var memberships =
            await _endUsersService.ListMembershipsForOrganizationAsync(caller, organization.Id, searchOptions,
                getOptions, cancellationToken);
        if (!memberships.IsSuccessful)
        {
            return memberships.Error;
        }

        return searchOptions.ApplyWithMetadata(memberships.Value.Results.ConvertAll(x => x.ToMember()));
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
            Ownership = organization.Ownership.ToEnumOrDefault(OrganizationOwnership.Shared)
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