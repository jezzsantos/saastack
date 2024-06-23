using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using {SubDomainName}Application.Persistence.ReadModels;
using {SubDomainName}Domain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace ProjectName.Persistence.ReadModels;

public class {SubDomainName}Projection : IReadModelProjection
{
    private readonly IReadModelStore<{SubDomainName}> _{SubDomainNameLower}s;

    public {SubDomainName}Projection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _{SubDomainNameLower}s = new ReadModelStore<{SubDomainName}>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof({SubDomainName}Root);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _{SubDomainNameLower}s.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                    },
                    cancellationToken);

            default:
                return false;
        }
    }
}