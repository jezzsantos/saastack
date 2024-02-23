using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using OrganizationsDomain;

namespace OrganizationsApplication.Persistence;

public interface IOrganizationRepository : IApplicationRepository
{
    Task<Result<OrganizationRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<OrganizationRoot, Error>> SaveAsync(OrganizationRoot organization, CancellationToken cancellationToken);
}