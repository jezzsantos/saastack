using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using {{SubdomainName | string.pascalplural}}Application.Persistence.ReadModels;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using Domain.Events.Shared.{{SubdomainName | string.pascalplural}};
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace {{SubdomainName | string.pascalplural}}Infrastructure.Persistence.ReadModels;

public class {{SubdomainName | string.pascalsingular}}Projection : IReadModelProjection
{
    private readonly IReadModelStore<{{SubdomainName | string.pascalsingular}}> _{{SubdomainName | string.pascalplural | string.downcase}};

    public {{SubdomainName | string.pascalsingular}}Projection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _{{SubdomainName | string.pascalplural | string.downcase}} = new ReadModelStore<{{SubdomainName | string.pascalsingular}}>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _{{SubdomainName | string.pascalplural | string.downcase}}.HandleCreateAsync(e.RootId, dto =>
                {
                    dto.OrganizationId = e.OrganizationId;
                    //TODO: add other assignments here
                }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof({{SubdomainName | string.pascalsingular}}Root);
}