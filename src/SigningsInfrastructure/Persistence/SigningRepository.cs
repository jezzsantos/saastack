using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using SigningsApplication.Persistence;
using SigningsApplication.Persistence.ReadModels;
using SigningsDomain;

namespace SigningsInfrastructure.Persistence;

public class SigningRepository : ISigningRepository
{
    private readonly ISnapshottingQueryStore<SigningRequest> _requestQueries;
    private readonly IEventSourcingDddCommandStore<SigningRequestRoot> _requests;

    public SigningRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<SigningRequestRoot> requestsStore, IDataStore store)
    {
        _requestQueries = new SnapshottingQueryStore<SigningRequest>(recorder, domainFactory, store);
        _requests = requestsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(_requestQueries.DestroyAllAsync(cancellationToken),
            _requests.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<SigningRequestRoot, Error>> SaveAsync(SigningRequestRoot request,
        CancellationToken cancellationToken)
    {
        return await SaveAsync(request, false, cancellationToken);
    }

    public async Task<Result<SigningRequestRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var request = await _requests.LoadAsync(id, cancellationToken);
        if (request.IsFailure)
        {
            return request.Error;
        }

        return request.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : request;
    }

    public async Task<Result<SigningRequestRoot, Error>> SaveAsync(SigningRequestRoot request, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _requests.SaveAsync(request, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(request.OrganizationId, request.Id, cancellationToken)
            : request;
    }
}