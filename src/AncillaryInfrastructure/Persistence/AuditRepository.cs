using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

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

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _auditQueries.DestroyAllAsync(cancellationToken),
            _audits.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _audits.SaveAsync(audit, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(audit.OrganizationId, audit.Id, cancellationToken)
            : audit;
    }

    public async Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, CancellationToken cancellationToken)
    {
        return await SaveAsync(audit, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Audit>, Error>> SearchAllAsync(DateTime? sinceUtc, string? organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var query = Query.From<Audit>().WhereNoOp();
        if (sinceUtc.HasValue)
        {
            query = query.AndWhere<DateTime?>(aud => aud.Created, ConditionOperator.GreaterThan, sinceUtc);
        }

        if (organizationId.HasValue())
        {
            query = query.AndWhere<string?>(aud => aud.OrganizationId, ConditionOperator.EqualTo, organizationId);
        }

        query = query.WithSearchOptions(searchOptions);

        var queried = await _auditQueries.QueryAsync(query, cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var audits = queried.Value.Results;
        return audits;
    }

    private async Task<Result<AuditRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var audit = await _audits.LoadAsync(id, cancellationToken);
        if (audit.IsFailure)
        {
            return audit.Error;
        }

        return audit.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : audit;
    }
}