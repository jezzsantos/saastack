using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IOAuth2ClientConsentRepository : IApplicationRepository
{
    Task<Result<Optional<OAuth2ClientConsentRoot>, Error>> FindByUserId(Identifier clientId, Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<OAuth2ClientConsentRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<OAuth2ClientConsentRoot, Error>> SaveAsync(OAuth2ClientConsentRoot client,
        CancellationToken cancellationToken);
}