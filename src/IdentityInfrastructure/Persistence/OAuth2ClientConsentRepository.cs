using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using IdentityApplication.Persistence;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

namespace IdentityInfrastructure.Persistence;

public class OAuth2ClientConsentRepository : IOAuth2ClientConsentRepository
{
    private readonly ISnapshottingQueryStore<OAuth2ClientConsent> _consentQueries;
    private readonly IEventSourcingDddCommandStore<OAuth2ClientConsentRoot> _consents;

    public OAuth2ClientConsentRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OAuth2ClientConsentRoot> consentsStore, IDataStore store)
    {
        _consentQueries = new SnapshottingQueryStore<OAuth2ClientConsent>(recorder, domainFactory, store);
        _consents = consentsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _consentQueries.DestroyAllAsync(cancellationToken),
            _consents.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<OAuth2ClientConsentRoot>, Error>> FindByUserId(Identifier clientId,
        Identifier userId, CancellationToken cancellationToken)
    {
        var query = Query.From<OAuth2ClientConsent>()
            .Where<string>(consent => consent.UserId, ConditionOperator.EqualTo, userId)
            .AndWhere<string>(consent => consent.ClientId, ConditionOperator.EqualTo, clientId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<OAuth2ClientConsentRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var consent = await _consents.LoadAsync(id, cancellationToken);
        if (consent.IsFailure)
        {
            return consent.Error;
        }

        return consent;
    }

    public async Task<Result<OAuth2ClientConsentRoot, Error>> SaveAsync(OAuth2ClientConsentRoot consent,
        CancellationToken cancellationToken)
    {
        var saved = await _consents.SaveAsync(consent, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return consent;
    }

    private async Task<Result<Optional<OAuth2ClientConsentRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<OAuth2ClientConsent> query,
        CancellationToken cancellationToken)
    {
        var queried = await _consentQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<OAuth2ClientConsentRoot>.None;
        }

        var consents = await _consents.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (consents.IsFailure)
        {
            return consents.Error;
        }

        return consents.Value.ToOptional();
    }
}