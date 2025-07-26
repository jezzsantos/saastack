using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IOAuth2ClientRepository : IApplicationRepository
{
    Task<Result<Optional<OAuth2ClientRoot>, Error>> FindById(Identifier id, CancellationToken cancellationToken);

    Task<Result<OAuth2ClientRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<OAuth2ClientRoot, Error>> SaveAsync(OAuth2ClientRoot client, CancellationToken cancellationToken);

    Task<Result<QueryResults<OAuth2Client>, Error>> SearchAllAsync(SearchOptions options,
        CancellationToken cancellationToken);
}