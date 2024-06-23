using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Persistence.ReadModels;

public class OrganizationProjection : IReadModelProjection
{
    private readonly IReadModelStore<Organization> _organizations;

    public OrganizationProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _organizations = new ReadModelStore<Organization>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(OrganizationRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _organizations.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.Name = e.Name;
                        dto.Ownership = e.Ownership;
                        dto.CreatedById = e.CreatedById;
                        dto.BillingSubscriberId = e.CreatedById; // a useful intermediary default
                    },
                    cancellationToken);

            case SettingCreated _:
                return true;

            case MembershipAdded _:
                return true;

            case MembershipRemoved _:
                return true;

            case MemberInvited _:
                return true;

            case MemberUnInvited _:
                return true;

            case AvatarAdded e:
                return await _organizations.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.AvatarImageId = e.AvatarId;
                        dto.AvatarUrl = e.AvatarUrl;
                    },
                    cancellationToken);

            case AvatarRemoved e:
                return await _organizations.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.AvatarImageId = Optional<string>.None;
                        dto.AvatarUrl = Optional<string>.None;
                    },
                    cancellationToken);

            case NameChanged e:
                return await _organizations.HandleUpdateAsync(e.RootId, dto => { dto.Name = e.Name; },
                    cancellationToken);

            case RoleAssigned _:
                return true;

            case RoleUnassigned _:
                return true;

            case BillingSubscribed e:
                return await _organizations.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.BillingSubscriberId = e.SubscriberId;
                        dto.BillingSubscriptionId = e.SubscriptionId;
                    },
                    cancellationToken);

            case BillingSubscriberChanged e:
                return await _organizations.HandleUpdateAsync(e.RootId,
                    dto => { dto.BillingSubscriberId = e.ToSubscriberId; },
                    cancellationToken);

            case Deleted e:
                return await _organizations.HandleDeleteAsync(e.RootId, cancellationToken);

            default:
                return false;
        }
    }
}