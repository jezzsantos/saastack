using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using IdentityApplication.Persistence;
using IdentityDomain;
using PersonName = Domain.Shared.PersonName;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a service to manage registered <see cref="ISSOAuthenticationProvider" />s
/// </summary>
public class SSOProvidersService : ISSOProvidersService
{
    private readonly IEnumerable<ISSOAuthenticationProvider> _authenticationProviders;
    private readonly IEncryptionService _encryptionService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly ISSOUsersRepository _repository;

    public SSOProvidersService(IRecorder recorder, IIdentifierFactory identifierFactory,
        IEncryptionService encryptionService,
        IEnumerable<ISSOAuthenticationProvider> authenticationProviders,
        ISSOUsersRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _encryptionService = encryptionService;
        _repository = repository;
        _authenticationProviders = authenticationProviders;
    }

    public Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByNameAsync(string name,
        CancellationToken cancellationToken)
    {
        var provider =
            _authenticationProviders.FirstOrDefault(provider => provider.ProviderName.EqualsIgnoreCase(name));
        return Task.FromResult<Result<Optional<ISSOAuthenticationProvider>, Error>>(provider.ToOptional());
    }

    public async Task<Result<Error>> SaveUserInfoAsync(string providerName, Identifier userId, SSOUserInfo userInfo,
        CancellationToken cancellationToken)
    {
        var retrievedProvider = await FindByNameAsync(providerName, cancellationToken);
        if (retrievedProvider.IsFailure)
        {
            return retrievedProvider.Error;
        }

        if (!retrievedProvider.Value.HasValue)
        {
            return Error.EntityNotFound(Resources.SSOProvidersService_UnknownProvider.Format(providerName));
        }

        var provider = retrievedProvider.Value.Value;
        var retrievedUser =
            await _repository.FindUserInfoByUserIdAsync(provider.ProviderName, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        SSOUserRoot user;
        if (retrievedUser.Value.HasValue)
        {
            user = retrievedUser.Value.Value;
        }
        else
        {
            var created = SSOUserRoot.Create(_recorder, _identifierFactory, _encryptionService, providerName, userId);
            if (created.IsFailure)
            {
                return created.Error;
            }

            user = created.Value;
        }

        var name = PersonName.Create(userInfo.FirstName, userInfo.LastName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var emailAddress = EmailAddress.Create(userInfo.EmailAddress);
        if (emailAddress.IsFailure)
        {
            return emailAddress.Error;
        }

        var timezone = Timezone.Create(userInfo.Timezone);
        if (timezone.IsFailure)
        {
            return timezone.Error;
        }

        var address = Address.Create(userInfo.CountryCode);
        if (address.IsFailure)
        {
            return address.Error;
        }

        var tokens = SSOAuthTokens.Create(userInfo.Tokens
            .Select(tok =>
                SSOAuthToken.Create(tok.Type.ToEnumOrDefault(SSOAuthTokenType.AccessToken), tok.Value, tok.ExpiresOn)
                    .Value)
            .ToList());
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var updated = user.UpdateDetails(tokens.Value, emailAddress.Value, name.Value, timezone.Value, address.Value);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }
}