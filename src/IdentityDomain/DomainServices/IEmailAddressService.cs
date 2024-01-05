using Domain.Common.ValueObjects;
using Domain.Shared;

namespace IdentityDomain.DomainServices;

public interface IEmailAddressService
{
    Task<bool> EnsureUniqueAsync(EmailAddress emailAddress, Identifier userId);
}