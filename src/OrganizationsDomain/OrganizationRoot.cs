using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

namespace OrganizationsDomain;

public sealed class OrganizationRoot : AggregateRootBase
{
    private readonly ITenantSettingService _tenantSettingService;

    public static Result<OrganizationRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService, Ownership ownership, Identifier createdBy, DisplayName name)
    {
        var root = new OrganizationRoot(recorder, idFactory, tenantSettingService);
        root.RaiseCreateEvent(OrganizationsDomain.Events.Created.Create(root.Id, ownership, createdBy, name));
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

    public Ownership Ownership { get; private set; }

    public Settings Settings { get; private set; } = Settings.Empty;

    public static AggregateRootFactory<OrganizationRoot> Rehydrate()
    {
        return (identifier, container, _) => new OrganizationRoot(container.Resolve<IRecorder>(),
            container.Resolve<IIdentifierFactory>(), container.Resolve<ITenantSettingService>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        //TODO: add your other invariant rules here

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.Created created:
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

            case Events.SettingCreated created:
            {
                var value = created.IsEncrypted
                    ? _tenantSettingService.Decrypt(created.Value)
                    : created.Value;

                var settings = Settings.AddOrUpdate(created.Name, value, created.IsEncrypted);
                if (!settings.IsSuccessful)
                {
                    return settings.Error;
                }

                Settings = settings.Value;
                Recorder.TraceDebug(null, "Organization {Id} created settings", Id);
                return Result.Ok;
            }

            case Events.SettingUpdated updated:
            {
                var to = updated.IsEncrypted
                    ? _tenantSettingService.Decrypt(updated.To)
                    : updated.To;

                var settings = Settings.AddOrUpdate(updated.Name, to, updated.IsEncrypted);
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

    public Result<Error> CreateSettings(Settings settings)
    {
        foreach (var (key, value) in settings.Properties)
        {
            var valueValue = value.IsEncrypted
                ? _tenantSettingService.Encrypt(value.Value)
                : value.Value;
            RaiseChangeEvent(OrganizationsDomain.Events.SettingCreated.Create(Id, key, valueValue, value.IsEncrypted));
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
                    ? _tenantSettingService.Encrypt(value.Value)
                    : value.Value;
                RaiseChangeEvent(OrganizationsDomain.Events.SettingUpdated.Create(Id, key, oldSetting!.Value,
                    valueValue, value.IsEncrypted));
            }
            else
            {
                var valueValue = value.IsEncrypted
                    ? _tenantSettingService.Encrypt(value.Value)
                    : value.Value;
                RaiseChangeEvent(
                    OrganizationsDomain.Events.SettingCreated.Create(Id, key, valueValue, value.IsEncrypted));
            }
        }

        return Result.Ok;
    }
}