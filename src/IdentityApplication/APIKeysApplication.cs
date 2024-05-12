using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared.DomainServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication;

public class APIKeysApplication : IAPIKeysApplication
{
    public static readonly TimeSpan DefaultAPIKeyExpiry = TimeSpan.FromHours(1);
    private readonly IAPIKeyHasherService _apiKeyHasherService;
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly IAPIKeysRepository _repository;
    private readonly ITokensService _tokensService;

    public APIKeysApplication(IRecorder recorder, IIdentifierFactory identifierFactory, ITokensService tokensService,
        IAPIKeyHasherService apiKeyHasherService, IEndUsersService endUsersService, IAPIKeysRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tokensService = tokensService;
        _apiKeyHasherService = apiKeyHasherService;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    public async Task<Result<Error>> DeleteAPIKeyAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var apiKey = retrieved.Value;
        var deleterId = caller.ToCallerId();
        var deleted = apiKey.Delete(deleterId);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(apiKey, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        apiKey = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "API key {Id} was deleted by {User}", apiKey.Id, deleterId);

        return Result.Ok;
    }

    public async Task<Result<Optional<EndUserWithMemberships>, Error>> FindMembershipsForAPIKeyAsync(
        ICallerContext caller, string apiKey,
        CancellationToken cancellationToken)
    {
        var keyToken = _tokensService.ParseApiKey(apiKey);
        if (!keyToken.HasValue)
        {
            return Error.EntityNotFound();
        }

        var retrievedApiKey = await _repository.FindByAPIKeyTokenAsync(keyToken.Value.Token, cancellationToken);
        if (retrievedApiKey.IsFailure)
        {
            return retrievedApiKey.Error;
        }

        if (!retrievedApiKey.Value.HasValue)
        {
            return Optional<EndUserWithMemberships>.None;
        }

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, retrievedApiKey.Value.Value.UserId,
                cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Optional<EndUserWithMemberships>.None;
        }

        var user = retrievedUser.Value;
        return user.ToOptional();
    }

    public async Task<Result<SearchResults<APIKey>, Error>> SearchAllAPIKeysForCallerAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var userId = caller.ToCallerId();
        var searched = await _repository.SearchAllForUserAsync(userId, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var apiKeys = searched.Value.Results;
        _recorder.TraceInformation(caller.ToCall(), "All keys were fetched for user {User}", userId);

        return searchOptions.ApplyWithMetadata(apiKeys.Select(key => key.ToApiKey()));
    }

#if TESTINGONLY
    public async Task<Result<APIKey, Error>> CreateAPIKeyForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await CreateAPIKeyAsync(caller, caller.CallerId, caller.CallerId, null, cancellationToken);
    }
#endif

    public async Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext caller, string userId,
        string description, DateTime? expiresOn, CancellationToken cancellationToken)
    {
        var keyToken = _tokensService.CreateAPIKey();

        var created = APIKeyRoot.Create(_recorder, _identifierFactory, _apiKeyHasherService, userId.ToId(), keyToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var apiKey = created.Value;
        var parameterized = apiKey.SetParameters(description,
            expiresOn ?? DateTime.UtcNow.ToNearestMinute().Add(DefaultAPIKeyExpiry));
        if (parameterized.IsFailure)
        {
            return parameterized.Error;
        }

        var saved = await _repository.SaveAsync(apiKey, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        apiKey = saved.Value;
        return apiKey.ToApiKey(keyToken.ApiKey, description);
    }
}

internal static class APIKeyConversionExtensions
{
    public static APIKey ToApiKey(this APIKeyRoot apiKey, string key, string description)
    {
        return new APIKey
        {
            Description = description,
            ExpiresOnUtc = apiKey.ExpiresOn,
            Key = key,
            UserId = apiKey.UserId,
            Id = apiKey.Id
        };
    }

    public static APIKey ToApiKey(this Persistence.ReadModels.APIKey apiKey)
    {
        return new APIKey
        {
            Description = apiKey.Description,
            ExpiresOnUtc = apiKey.ExpiresOn,
            Key = apiKey.KeyToken,
            UserId = apiKey.UserId,
            Id = apiKey.Id
        };
    }
}