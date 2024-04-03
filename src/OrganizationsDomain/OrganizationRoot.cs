using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using Domain.Shared.Organizations;

namespace OrganizationsDomain;

public delegate Task<Result<Error>> Callback();

public sealed class OrganizationRoot : AggregateRootBase
{
    private readonly ITenantSettingService _tenantSettingService;

    public static Result<OrganizationRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService, OrganizationOwnership ownership, Identifier createdBy,
        DisplayName name)
    {
        var root = new OrganizationRoot(recorder, idFactory, tenantSettingService);
        root.RaiseCreateEvent(OrganizationsDomain.Events.Created(root.Id, ownership, createdBy, name));
        return root;
    }

    private OrganizationRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService) :
        base(recorder, idFactory)
    {
        _tenantSettingService = tenantSettingService;
    }

    private OrganizationRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _tenantSettingService = tenantSettingService;
    }

    public Identifier CreatedById { get; private set; } = Identifier.Empty();

    public DisplayName Name { get; private set; } = DisplayName.Empty;

    public OrganizationOwnership Ownership { get; private set; }

    public Settings Settings { get; private set; } = Settings.Empty;

    public static AggregateRootFactory<OrganizationRoot> Rehydrate()
    {
        return (identifier, container, _) => new OrganizationRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), container.GetRequiredService<ITenantSettingService>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                var name = DisplayName.Create(created.Name);
                if (!name.IsSuccessful)
                {
                    return name.Error;
                }

                Name = name.Value;
                Ownership = created.Ownership;
                CreatedById = created.CreatedById.ToId();
                return Result.Ok;
            }

            case SettingCreated created:
            {
                var value = Setting.From(created.StringValue, created.ValueType, created.IsEncrypted,
                    _tenantSettingService);
                if (!value.IsSuccessful)
                {
                    return value.Error;
                }

                var settings = Settings.AddOrUpdate(created.Name, value.Value);
                if (!settings.IsSuccessful)
                {
                    return settings.Error;
                }

                Settings = settings.Value;
                Recorder.TraceDebug(null, "Organization {Id} created settings", Id);
                return Result.Ok;
            }

            case SettingUpdated updated:
            {
                var to = Setting.From(updated.To, updated.ToType, updated.IsEncrypted, _tenantSettingService);
                if (!to.IsSuccessful)
                {
                    return to.Error;
                }

                var settings = Settings.AddOrUpdate(updated.Name, to.Value);
                if (!settings.IsSuccessful)
                {
                    return settings.Error;
                }

                Settings = settings.Value;
                Recorder.TraceDebug(null, "Organization {Id} created settings", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public async Task<Result<Error>> AddMembershipAsync(Identifier inviterId, Roles inviterRoles, Callback onPermitted)
    {
        if (!IsOwner(inviterRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_AddMembership_NotOrgOwner);
        }

        return await onPermitted();
    }

    public Result<Error> CreateSettings(Settings settings)
    {
        foreach (var (key, value) in settings.Properties)
        {
            var valueValue = value.IsEncrypted
                ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                : value.Value.ToString() ?? string.Empty;
            RaiseChangeEvent(OrganizationsDomain.Events.SettingCreated(Id, key, valueValue, value.ValueType,
                value.IsEncrypted));
        }

        return Result.Ok;
    }

    public Result<Error> UpdateSettings(Settings settings)
    {
        foreach (var (key, value) in settings.Properties)
        {
            if (Settings.TryGet(key, out var oldSetting))
            {
                var valueValue = value.IsEncrypted
                    ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                    : value.Value.ToString() ?? string.Empty;
                var oldValue = oldSetting!.Value.ToString() ?? string.Empty;
                RaiseChangeEvent(OrganizationsDomain.Events.SettingUpdated(Id, key, oldValue,
                    oldSetting.ValueType,
                    valueValue, value.ValueType, value.IsEncrypted));
            }
            else
            {
                var valueValue = value.IsEncrypted
                    ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                    : value.Value.ToString() ?? string.Empty;
                RaiseChangeEvent(
                    OrganizationsDomain.Events.SettingCreated(Id, key, valueValue, value.ValueType,
                        value.IsEncrypted));
            }
        }

        return Result.Ok;
    }

    private static bool IsOwner(Roles roles)
    {
        return roles.HasRole(TenantRoles.Owner);
    }
}