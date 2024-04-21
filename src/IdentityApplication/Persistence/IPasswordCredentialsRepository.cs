using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IPasswordCredentialsRepository : IApplicationRepository
{
    Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByPasswordResetTokenAsync(string token,
        CancellationToken cancellationToken);

    Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByRegistrationVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken);

    Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<Optional<PasswordCredentialRoot>, Error>> FindCredentialsByUsernameAsync(string username,
        CancellationToken cancellationToken);

    Task<Result<PasswordCredentialRoot, Error>> SaveAsync(PasswordCredentialRoot credential,
        CancellationToken cancellationToken);
}