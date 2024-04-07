using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
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
    private readonly IReadModelProjectionStore<Organization> _organizations;

    public OrganizationProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _organizations = new ReadModelProjectionStore<Organization>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(OrganizationRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _organizations.HandleCreateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.Name = e.Name;
                        dto.Ownership = e.Ownership;
                        dto.CreatedById = e.CreatedById;
                    },
                    cancellationToken);

            case SettingCreated _:
                return true;

            case MembershipAdded _:
                return true;

            default:
                return false;
        }
    }
}