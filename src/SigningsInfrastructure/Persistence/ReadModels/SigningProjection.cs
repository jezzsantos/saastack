using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Signings;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using SigningsApplication.Persistence.ReadModels;
using SigningsDomain;

namespace SigningsInfrastructure.Persistence.ReadModels;

public class SigningProjection : IReadModelProjection
{
    private readonly IReadModelStore<SigningRequest> _signatures;

    public SigningProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _signatures = new ReadModelStore<SigningRequest>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(SigningRequestRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _signatures.HandleCreateAsync(e.RootId, dto => { dto.OrganizationId = e.OrganizationId; },
                    cancellationToken);

            default:
                return false;
        }
    }
}