using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;

namespace AncillaryApplication.Persistence;

public interface IAuditRepository : IApplicationRepository
{
    Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, bool reload, CancellationToken cancellationToken);

    Task<Result<AuditRoot, Error>> SaveAsync(AuditRoot audit, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Audit>, Error>> SearchAllAsync(DateTime? sinceUtc, string? organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken);
}