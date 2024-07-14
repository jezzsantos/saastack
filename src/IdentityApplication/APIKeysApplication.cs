using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication;

public class APIKeysApplication : IAPIKeysApplication
{
    private const string ProviderName = "apikey";
    public static readonly TimeSpan DefaultAPIKeyExpiry = TimeSpan.FromHours(1);
    private readonly IAPIKeyHasherService _apiKeyHasherService;
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly IAPIKeysRepository _repository;
    private readonly ITokensService _tokensService;
    private readonly IUserProfilesService _userProfilesService;

    public APIKeysApplication(IRecorder recorder, IIdentifierFactory identifierFactory, ITokensService tokensService,
        IAPIKeyHasherService apiKeyHasherService, IEndUsersService endUsersService,
        IUserProfilesService userProfilesService, IAPIKeysRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tokensService = tokensService;
        _apiKeyHasherService = apiKeyHasherService;
        _endUsersService = endUsersService;
        _userProfilesService = userProfilesService;
        _repository = repository;
    }

    public async Task<Result<EndUserWithMemberships, Error>> AuthenticateAsync(
        ICallerContext caller, string apiKey, CancellationToken cancellationToken)
    {
        var keyToken = _tokensService.ParseApiKey(apiKey);
        if (!keyToken.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var retrievedApiKey = await _repository.FindByAPIKeyTokenAsync(keyToken.Value.Token, cancellationToken);
        if (retrievedApiKey.IsFailure)
        {
            return retrievedApiKey.Error;
        }

        if (!retrievedApiKey.Value.HasValue)
        {
            return Error.NotAuthenticated();
        }

        var retrievedUser =
            await _endUsersService.GetMembershipsPrivateAsync(caller, retrievedApiKey.Value.Value.UserId,
                cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var user = retrievedUser.Value;
        if (user.Status != EndUserStatus.Registered)
        {
            return Error.NotAuthenticated();
        }

        if (user.Access == EndUserAccess.Suspended)
        {
            _recorder.AuditAgainst(caller.ToCall(), user.Id,
                Audits.APIKeysApplication_Authenticate_AccountSuspended,
                "User {Id} tried to authenticate a APIKey with a suspended account", user.Id);
            return Error.EntityExists(Resources.APIKeysApplication_AccountSuspended);
        }

        var maintenance = Caller.CreateAsMaintenance(caller.CallId);
        var profiled = await _userProfilesService.GetProfilePrivateAsync(maintenance, user.Id, cancellationToken);
        if (profiled.IsFailure)
        {
            return profiled.Error;
        }

        var profile = profiled.Value;
        _recorder.AuditAgainst(caller.ToCall(), user.Id,
            Audits.APIKeysApplication_Authenticate_Succeeded,
            "User {Id} succeeded to authenticate with APIKey", user.Id);
        _recorder.TrackUsageFor(caller.ToCall(), user.Id, UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            user.ToLoginUserUsage(ProviderName, profile));

        return user;
    }

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

#if TESTINGONLY
    public async Task<Result<APIKey, Error>> CreateAPIKeyForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await CreateAPIKeyAsync(caller, caller.CallerId, caller.CallerId, null, cancellationToken);
    }
#endif

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