using Domain.Common.ValueObjects;
using Domain.Shared;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.DomainServices;

public sealed class EmailAddressService : IEmailAddressService
{
    private readonly IPasswordCredentialsRepository _repository;

    public EmailAddressService(IPasswordCredentialsRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> EnsureUniqueAsync(EmailAddress emailAddress, Identifier userId)
    {
        var retrieved = await _repository.FindCredentialsByUsernameAsync(emailAddress.Address, CancellationToken.None);
        if (!retrieved.IsSuccessful)
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