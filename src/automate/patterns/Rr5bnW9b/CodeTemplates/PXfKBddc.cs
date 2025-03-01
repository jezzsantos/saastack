using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using {{SubdomainName | string.pascalplural}}Application.Persistence;
using {{SubdomainName | string.pascalplural}}Application.Persistence.ReadModels;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

namespace {{SubdomainName | string.pascalplural}}Infrastructure.Persistence;

public class {{SubdomainName | string.pascalsingular}}Repository : I{{SubdomainName | string.pascalsingular}}Repository
{
    private readonly ISnapshottingQueryStore<{{SubdomainName | string.pascalsingular}}> _{{SubdomainName | string.pascalsingular | string.downcase}}Queries;
    private readonly IEventSourcingDddCommandStore<{{SubdomainName | string.pascalsingular}}Root> _{{SubdomainName | string.pascalplural | string.downcase}};

    public {{SubdomainName | string.pascalsingular}}Repository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<{{SubdomainName | string.pascalsingular}}Root> {{SubdomainName | string.pascalplural | string.downcase}}Store, IDataStore store)
    {
        _{{SubdomainName | string.pascalsingular | string.downcase}}Queries = new SnapshottingQueryStore<{{SubdomainName | string.pascalsingular}}>(recorder, domainFactory, store);
        _{{SubdomainName | string.pascalplural | string.downcase}} = {{SubdomainName | string.pascalplural | string.downcase}}Store;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _{{SubdomainName | string.pascalsingular | string.downcase}}Queries.DestroyAllAsync(cancellationToken),
            _{{SubdomainName | string.pascalplural | string.downcase}}.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalsingular | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}.LoadAsync(id, cancellationToken);
        if ({{SubdomainName | string.pascalsingular | string.downcase}}.IsFailure)
        {
            return {{SubdomainName | string.pascalsingular | string.downcase}}.Error;
        }

        return {{SubdomainName | string.pascalsingular | string.downcase}}.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : {{SubdomainName | string.pascalsingular | string.downcase}};
    }

    public async Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> SaveAsync({{SubdomainName | string.pascalsingular}}Root {{SubdomainName | string.pascalsingular | string.downcase}}, bool reload, CancellationToken cancellationToken)
    {
        var saved = await _{{SubdomainName | string.pascalplural | string.downcase}}.SaveAsync({{SubdomainName | string.pascalsingular | string.downcase}}, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync({{SubdomainName | string.pascalsingular | string.downcase}}.OrganizationId, {{SubdomainName | string.pascalsingular | string.downcase}}.Id, cancellationToken)
            : {{SubdomainName | string.pascalsingular | string.downcase}};
    }

    public async Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> SaveAsync({{SubdomainName | string.pascalsingular}}Root {{SubdomainName | string.pascalsingular | string.downcase}}, CancellationToken cancellationToken)
    {
        return await SaveAsync({{SubdomainName | string.pascalsingular | string.downcase}}, false, cancellationToken);
    }
}