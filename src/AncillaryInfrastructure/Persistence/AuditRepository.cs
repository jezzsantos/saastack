using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = Common.Extensions.Task;

namespace AncillaryInfrastructure.Persistence;

public class AuditRepository : IAuditRepository
{
    private readonly ISnapshottingQueryStore<Audit> _auditQueries;
    private readonly IEventSourcingDddCommandStore<AuditRoot> _audits;

    public AuditRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<AuditRoot> auditsStore, IDataStore store)
    {
        _auditQueries = new SnapshottingQueryStore<Audit>(recorder, domainFactory, store);
        _audits = auditsStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Task.WhenAllAsync(
            _auditQueries.DestroyAllAsync(cancellationToken),
            _audits.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, bool reload,
        CancellationToken cancellationToken)
    {
        await _audits.SaveAsync(audit, cancellationToken);

        return reload
            ? await LoadAsync(audit.OrganizationId, audit.Id, cancellationToken)
            : audit;
    }

    public async Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, CancellationToken cancellationToken)
    {
        return await SaveAsync(audit, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Audit>, Error>> SearchAllAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var audits = await _auditQueries.QueryAsync(Query.From<Audit>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (!audits.IsSuccessful)
        {
            return audits.Error;
        }

        return audits.Value.Results;
    }

    public async Task<Result<AuditRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var audit = await _audits.LoadAsync(id, cancellationToken);
        if (!audit.IsSuccessful)
        {
            return audit.Error;
        }

        return audit.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : audit;
    }
}