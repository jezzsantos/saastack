using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
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

    public Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByProviderNameAsync(string providerName,
        CancellationToken cancellationToken)
    {
        var provider =
            _authenticationProviders.FirstOrDefault(provider => provider.ProviderName.EqualsIgnoreCase(providerName));
        return Task.FromResult<Result<Optional<ISSOAuthenticationProvider>, Error>>(provider.ToOptional());
    }

    public async Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByUserIdAsync(ICallerContext caller,
        string userId, string providerName, CancellationToken cancellationToken)
    {
        var provider =
            _authenticationProviders.FirstOrDefault(provider => provider.ProviderName.EqualsIgnoreCase(providerName));
        if (provider.NotExists())
        {
            return Optional<ISSOAuthenticationProvider>.None;
        }

        var retrieved = await _repository.FindByUserIdAsync(provider.ProviderName, userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Optional<ISSOAuthenticationProvider>.None;
        }

        var user = retrieved.Value.Value;

        var viewed = user.ViewUser(caller.ToCallerId());
        if (viewed.IsFailure)
        {
            return viewed.Error;
        }

        return provider.ToOptional();
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await GetTokensInternalAsync(caller.ToCallerId(), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensOnBehalfOfUserAsync(
        ICallerContext caller, string userId, CancellationToken cancellationToken)
    {
        return await GetTokensInternalAsync(userId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> SaveUserInfoAsync(ICallerContext caller, string providerName, string userId,
        SSOUserInfo userInfo, CancellationToken cancellationToken)
    {
        var retrievedProvider = await FindByProviderNameAsync(providerName, cancellationToken);
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
            await _repository.FindByUserIdAsync(provider.ProviderName, userId.ToId(), cancellationToken);
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
            var created = SSOUserRoot.Create(_recorder, _identifierFactory, providerName, userId.ToId());
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

        var toks = userInfo.Tokens.ToAuthTokens(_encryptionService);
        if (toks.IsFailure)
        {
            return toks.Error;
        }

        var tokens = SSOAuthTokens.Create(toks.Value);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var updated = user.AddDetails(tokens.Value, emailAddress.Value, name.Value, timezone.Value, address.Value);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        user = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "SSO User {UserId} updated with user information",
            user.UserId);

        return Result.Ok;
    }

    public async Task<Result<Error>> SaveUserTokensAsync(ICallerContext caller, string providerName, string userId,
        ProviderAuthenticationTokens tokens, CancellationToken cancellationToken)
    {
        var retrievedProvider = await FindByProviderNameAsync(providerName, cancellationToken);
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
            await _repository.FindByUserIdAsync(provider.ProviderName, userId.ToId(), cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        if (!retrievedUser.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var user = retrievedUser.Value.Value;
        var toks = tokens.ToAuthTokens(_encryptionService);
        if (toks.IsFailure)
        {
            return toks.Error;
        }

        var ssoTokens = SSOAuthTokens.Create(toks.Value);
        if (ssoTokens.IsFailure)
        {
            return ssoTokens.Error;
        }

        var changed = user.ChangeTokens(caller.ToCallerId(), ssoTokens.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        user = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "SSO User {UserId} changed tokens",
            user.UserId);

        return Result.Ok;
    }

    private async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensInternalAsync(
        Identifier userId, CancellationToken cancellationToken)
    {
        var allTokens = new List<ProviderAuthenticationTokens>();
        foreach (var provider in _authenticationProviders)
        {
            var retrievedUser = await _repository.FindByUserIdAsync(provider.ProviderName, userId, cancellationToken);
            if (retrievedUser.IsFailure)
            {
                return retrievedUser.Error;
            }

            if (!retrievedUser.Value.HasValue)
            {
                continue;
            }

            var user = retrievedUser.Value.Value;
            var viewed = user.ViewUser(userId);
            if (viewed.IsFailure)
            {
                return viewed.Error;
            }

            if (!user.Tokens.HasValue)
            {
                continue;
            }

            var tokens = user.Tokens.Value;
            allTokens.Add(tokens.ToProviderAuthenticationTokens(provider.ProviderName, _encryptionService));
        }

        return allTokens;
    }
}

internal static class SSOProvidersServiceConversionExtensions
{
    public static Result<List<SSOAuthToken>, Error> ToAuthTokens(this IReadOnlyList<AuthToken> tokens,
        IEncryptionService encryptionService)
    {
        var list = new List<SSOAuthToken>();
        foreach (var token in tokens)
        {
            var tok = SSOAuthToken.Create(token.Type.ToEnumOrDefault(SSOAuthTokenType.AccessToken), token.Value,
                token.ExpiresOn,
                encryptionService);
            if (tok.IsFailure)
            {
                return tok.Error;
            }

            list.Add(tok.Value);
        }

        return list;
    }

    public static Result<List<SSOAuthToken>, Error> ToAuthTokens(this ProviderAuthenticationTokens tokens,
        IEncryptionService encryptionService)
    {
        var list = new List<SSOAuthToken>();
        if (tokens.AccessToken.Exists())
        {
            var tok = SSOAuthToken.Create(SSOAuthTokenType.AccessToken, tokens.AccessToken.Value,
                tokens.AccessToken.ExpiresOn, encryptionService);
            if (tok.IsFailure)
            {
                return tok.Error;
            }

            list.Add(tok.Value);
        }

        if (tokens.RefreshToken.Exists())
        {
            var tok = SSOAuthToken.Create(SSOAuthTokenType.RefreshToken, tokens.RefreshToken.Value,
                tokens.RefreshToken.ExpiresOn, encryptionService);
            if (tok.IsFailure)
            {
                return tok.Error;
            }

            list.Add(tok.Value);
        }

        if (tokens.OtherTokens.HasAny())
        {
            foreach (var token in tokens.OtherTokens)
            {
                var tok = SSOAuthToken.Create(SSOAuthTokenType.OtherToken, token.Value,
                    token.ExpiresOn, encryptionService);
                if (tok.IsFailure)
                {
                    return tok.Error;
                }

                list.Add(tok.Value);
            }
        }

        return list;
    }

    public static ProviderAuthenticationTokens ToProviderAuthenticationTokens(this SSOAuthTokens tokens,
        string providerName, IEncryptionService encryptionService)
    {
        var accessToken = tokens
            .ToList()
            .Single(tok => tok.Type == SSOAuthTokenType.AccessToken);
        var refreshToken = tokens
            .ToList()
            .FirstOrDefault(tok => tok.Type == SSOAuthTokenType.RefreshToken);
        var otherTokens = tokens
            .ToList()
            .Where(tok => tok.Type == SSOAuthTokenType.OtherToken)
            .ToList();

        var providerTokens = new ProviderAuthenticationTokens
        {
            Provider = providerName,
            AccessToken = new AuthenticationToken
            {
                ExpiresOn = accessToken.ExpiresOn,
                Type = TokenType.AccessToken,
                Value = encryptionService.Decrypt(accessToken.EncryptedValue)
            },
            RefreshToken = refreshToken.Exists()
                ? new AuthenticationToken
                {
                    ExpiresOn = refreshToken.ExpiresOn,
                    Type = TokenType.RefreshToken,
                    Value = encryptionService.Decrypt(refreshToken.EncryptedValue)
                }
                : null,
            OtherTokens = otherTokens.HasAny()
                ? otherTokens.Select(otherToken => new AuthenticationToken
                {
                    ExpiresOn = otherToken.ExpiresOn,
                    Type = TokenType.OtherToken,
                    Value = encryptionService.Decrypt(otherToken.EncryptedValue)
                }).ToList()
                : []
        };

        return providerTokens;
    }
}