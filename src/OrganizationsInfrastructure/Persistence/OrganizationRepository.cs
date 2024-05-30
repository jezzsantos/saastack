using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using OrganizationsApplication.Persistence;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Persistence;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly ISnapshottingQueryStore<Organization> _organizationQueries;
    private readonly IEventSourcingDddCommandStore<OrganizationRoot> _organizations;

    public OrganizationRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OrganizationRoot> organizationsStore, IDataStore store)
    {
        _organizationQueries = new SnapshottingQueryStore<Organization>(recorder, domainFactory, store);
        _organizations = organizationsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _organizationQueries.DestroyAllAsync(cancellationToken),
            _organizations.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<OrganizationRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var organization = await _organizations.LoadAsync(id, cancellationToken);
        if (organization.IsFailure)
        {
            return organization.Error;
        }

        return organization;
    }

    public async Task<Result<OrganizationRoot, Error>> SaveAsync(OrganizationRoot organization,
        CancellationToken cancellationToken)
    {
        var saved = await _organizations.SaveAsync(organization, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return organization;
    }
}