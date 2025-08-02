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
using AuthToken = Application.Resources.Shared.AuthToken;
using PersonName = Domain.Shared.PersonName;
using SSOUser = Application.Resources.Shared.SSOUser;

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
        IEncryptionService encryptionService, IEnumerable<ISSOAuthenticationProvider> authenticationProviders,
        ISSOUsersRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _encryptionService = encryptionService;
        _repository = repository;
        _authenticationProviders = authenticationProviders;
    }

    public async Task<Result<SSOAuthUserInfo, Error>> AuthenticateUserAsync(ICallerContext caller, string providerName,
        string authCode, string? username, CancellationToken cancellationToken)
    {
        var retrievedProvider = FindProviderByNameInternal(providerName);
        if (retrievedProvider.IsFailure)
        {
            return retrievedProvider.Error;
        }

        if (!retrievedProvider.Value.HasValue)
        {
            return Error.EntityNotFound(Resources.SSOProvidersService_UnknownProvider.Format(providerName));
        }

        var provider = retrievedProvider.Value.Value;
        var authenticated = await provider.AuthenticateAsync(caller, authCode, username, cancellationToken);
        if (authenticated.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var userInfo = authenticated.Value;
        if (userInfo.UId.HasNoValue())
        {
            return Error.Validation(Resources.SSOProvidersService_Authentication_MissingUid);
        }

        var email = EmailAddress.Create(userInfo.EmailAddress);
        if (email.IsFailure)
        {
            return Error.Validation(Resources.SSOProvidersService_Authentication_InvalidEmailAddress);
        }

        var name = PersonName.Create(userInfo.FirstName, userInfo.LastName);
        if (name.IsFailure)
        {
            return Error.Validation(Resources.SSOProvidersService_Authentication_InvalidNames);
        }

        return userInfo;
    }

    public async Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindProviderByUserIdAsync(
        ICallerContext caller, string userId, string providerName, CancellationToken cancellationToken)
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

        var viewed = user.ViewUser(userId.ToId());
        if (viewed.IsFailure)
        {
            return viewed.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "SSO Provider {Provider} retrieved", provider.ProviderName);

        return provider.ToOptional();
    }

    public async Task<Result<Optional<SSOUser>, Error>> FindUserByProviderAsync(ICallerContext caller,
        string providerName, SSOAuthUserInfo authUserInfo, CancellationToken cancellationToken)
    {
        var retrievedProvider = FindProviderByNameInternal(providerName);
        if (retrievedProvider.IsFailure)
        {
            return retrievedProvider.Error;
        }

        if (!retrievedProvider.Value.HasValue)
        {
            return Error.EntityNotFound(Resources.SSOProvidersService_UnknownProvider.Format(providerName));
        }

        var retrievedUser = await _repository.FindByProviderUIdAsync(providerName, authUserInfo.UId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        if (!retrievedUser.Value.HasValue)
        {
            return Optional<SSOUser>.None;
        }

        var user = retrievedUser.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "SSO User {UserId} retrieved", user.UserId);

        return user.ToUser().ToOptional();
    }

    public async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensForUserAsync(
        ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        return await GetProviderTokensInternalAsync(userId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> SaveInfoOnBehalfOfUserAsync(ICallerContext caller, string providerName,
        string userId, SSOAuthUserInfo authUserInfo, CancellationToken cancellationToken)
    {
        var retrievedProvider = FindProviderByNameInternal(providerName);
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

        var name = PersonName.Create(authUserInfo.FirstName, authUserInfo.LastName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var emailAddress = EmailAddress.Create(authUserInfo.EmailAddress);
        if (emailAddress.IsFailure)
        {
            return emailAddress.Error;
        }

        var timezone = Timezone.Create(authUserInfo.Timezone);
        if (timezone.IsFailure)
        {
            return timezone.Error;
        }

        var address = Address.Create(authUserInfo.CountryCode);
        if (address.IsFailure)
        {
            return address.Error;
        }

        var providerUniqueId = authUserInfo.UId;
        var changed = user.ChangeDetails(providerUniqueId, emailAddress.Value, name.Value, timezone.Value,
            address.Value);
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
        _recorder.TraceInformation(caller.ToCall(), "SSO User {UserId} updated with user information",
            user.UserId);

        return await SaveProviderTokensAsync(caller, user.ProviderName.Value, userId.ToId(),
            authUserInfo.Tokens, cancellationToken);
    }

    public async Task<Result<Error>> SaveTokensOnBehalfOfUserAsync(ICallerContext caller, string providerName,
        string userId, ProviderAuthenticationTokens tokens, CancellationToken cancellationToken)
    {
        var retrievedProvider = FindProviderByNameInternal(providerName);
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

        var authTokens = tokens.ToAuthTokens();
        if (authTokens.IsFailure)
        {
            return authTokens.Error;
        }

        return await SaveProviderTokensAsync(caller, provider.ProviderName, userId.ToId(),
            authTokens.Value, cancellationToken);
    }

    private async Task<Result<Error>> SaveProviderTokensAsync(ICallerContext caller, string providerName,
        Identifier userId, IReadOnlyList<AuthToken> authTokens, CancellationToken cancellationToken)
    {
        var retrievedProviderTokens =
            await _repository.FindProviderTokensByUserIdAndProviderAsync(providerName, userId,
                cancellationToken);
        if (retrievedProviderTokens.IsFailure)
        {
            return retrievedProviderTokens.Error;
        }

        ProviderAuthTokensRoot providerAuthTokens;
        if (!retrievedProviderTokens.Value.HasValue)
        {
            var created =
                ProviderAuthTokensRoot.Create(_recorder, _identifierFactory, providerName, userId);
            if (created.IsFailure)
            {
                return created.Error;
            }

            providerAuthTokens = created.Value;
        }
        else
        {
            providerAuthTokens = retrievedProviderTokens.Value.Value;
        }

        var toks = authTokens.ToAuthTokens(_encryptionService);
        if (toks.IsFailure)
        {
            return toks.Error;
        }

        var changed = providerAuthTokens.ChangeTokens(userId, toks.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var savedTokens = await _repository.SaveAsync(providerAuthTokens, cancellationToken);
        if (savedTokens.IsFailure)
        {
            return savedTokens.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "SSO User {UserId} updated tokens for provider {Provider}",
            userId, providerName);

        return Result.Ok;
    }

    private Result<Optional<ISSOAuthenticationProvider>, Error> FindProviderByNameInternal(string providerName)
    {
        var provider =
            _authenticationProviders.FirstOrDefault(provider => provider.ProviderName.EqualsIgnoreCase(providerName));
        return provider.ToOptional();
    }

    private async Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetProviderTokensInternalAsync(
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

            var providerTokens =
                await _repository.FindProviderTokensByUserIdAndProviderAsync(provider.ProviderName, user.UserId,
                    cancellationToken);
            if (providerTokens.IsFailure)
            {
                continue;
            }

            if (!providerTokens.Value.HasValue)
            {
                continue;
            }

            allTokens.Add(providerTokens.Value.Value.ToProviderAuthenticationTokens(_encryptionService));
        }

        return allTokens;
    }
}

internal static class SSOProvidersServiceConversionExtensions
{
    public static Result<AuthTokens, Error> ToAuthTokens(this IReadOnlyList<AuthToken> authTokens,
        IEncryptionService encryptionService)
    {
        var list = new List<IdentityDomain.AuthToken>();
        foreach (var token in authTokens)
        {
            var tok = IdentityDomain.AuthToken.Create(token.Type.ToEnumOrDefault(AuthTokenType.AccessToken),
                token.Value,
                token.ExpiresOn, encryptionService);
            if (tok.IsFailure)
            {
                return tok.Error;
            }

            list.Add(tok.Value);
        }

        return AuthTokens.Create(list);
    }

    public static Result<IReadOnlyList<AuthToken>, Error> ToAuthTokens(this ProviderAuthenticationTokens providerTokens)
    {
        var list = new List<AuthToken>();
        if (providerTokens.AccessToken.Exists())
        {
            list.Add(providerTokens.AccessToken.ToAuthToken());
        }

        if (providerTokens.RefreshToken.Exists())
        {
            list.Add(providerTokens.RefreshToken.ToAuthToken());
        }

        if (providerTokens.OtherTokens.HasAny())
        {
            foreach (var token in providerTokens.OtherTokens)
            {
                list.Add(token.ToAuthToken());
            }
        }

        return list;
    }

    public static ProviderAuthenticationTokens ToProviderAuthenticationTokens(
        this ProviderAuthTokensRoot providerTokens, IEncryptionService encryptionService)
    {
        var authTokens = providerTokens.Tokens.Value;
        var accessToken = authTokens.GetToken(AuthTokenType.AccessToken).Value;
        var refreshToken = authTokens.GetToken(AuthTokenType.RefreshToken).Value;
        var otherToken = authTokens.GetToken(AuthTokenType.OtherToken);

        var authenticationTokens = new ProviderAuthenticationTokens
        {
            Provider = providerTokens.ProviderName,
            AccessToken = new AuthenticationToken
            {
                ExpiresOn = accessToken.ExpiresOn,
                Type = TokenType.AccessToken,
                Value = accessToken.GetDecryptedValue(encryptionService)
            },
            RefreshToken = refreshToken.Exists()
                ? new AuthenticationToken
                {
                    ExpiresOn = refreshToken.ExpiresOn,
                    Type = TokenType.RefreshToken,
                    Value = refreshToken.GetDecryptedValue(encryptionService)
                }
                : null,
            OtherTokens = otherToken.HasValue
                ?
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = otherToken.Value.ExpiresOn,
                        Type = TokenType.OtherToken,
                        Value = otherToken.Value.GetDecryptedValue(encryptionService)
                    }
                ]
                : []
        };

        return authenticationTokens;
    }

    public static SSOUser ToUser(this SSOUserRoot user)
    {
        return new SSOUser
        {
            Id = user.UserId,
            ProviderUId = user.ProviderUId
        };
    }

    private static AuthToken ToAuthToken(this AuthenticationToken token)
    {
        return new AuthToken(token.Type, token.Value, token.ExpiresOn);
    }
}