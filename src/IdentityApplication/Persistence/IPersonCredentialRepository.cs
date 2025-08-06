using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IPersonCredentialRepository : IApplicationRepository
{
    Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByMfaAuthenticationTokenAsync(string token,
        CancellationToken cancellationToken);

    Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByPasswordResetTokenAsync(string token,
        CancellationToken cancellationToken);

    Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialByUsernameAsync(string username,
        CancellationToken cancellationToken);

    Task<Result<Optional<PersonCredentialRoot>, Error>> FindCredentialsByRegistrationVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken);

    Task<Result<PersonCredentialRoot, Error>> SaveAsync(PersonCredentialRoot personCredential,
        CancellationToken cancellationToken);

    Task<Result<PersonCredentialRoot, Error>> SaveAsync(PersonCredentialRoot personCredential, bool reload,
        CancellationToken cancellationToken);
}