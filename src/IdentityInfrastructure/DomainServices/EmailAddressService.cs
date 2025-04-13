using Domain.Common.ValueObjects;
using Domain.Shared;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.DomainServices;

public sealed class EmailAddressService : IEmailAddressService
{
    private readonly IPersonCredentialRepository _repository;

    public EmailAddressService(IPersonCredentialRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> EnsureUniqueAsync(EmailAddress emailAddress, Identifier userId)
    {
        var retrieved = await _repository.FindCredentialByUsernameAsync(emailAddress.Address, CancellationToken.None);
        if (retrieved.IsFailure)
        {
            return false;
        }

        var credential = retrieved.Value;
        if (credential.HasValue)
        {
            return credential.Value.UserId.Equals(userId);
        }

        return true;
    }
}