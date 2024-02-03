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
    private readonly IAPIKeyRepository _repository;
    private readonly ITokensService _tokensService;

    public APIKeysApplication(IRecorder recorder, IIdentifierFactory identifierFactory, ITokensService tokensService,
        IAPIKeyHasherService apiKeyHasherService, IEndUsersService endUsersService, IAPIKeyRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tokensService = tokensService;
        _apiKeyHasherService = apiKeyHasherService;
        _endUsersService = endUsersService;
        _repository = repository;
    }

    public async Task<Result<Optional<EndUserWithMemberships>, Error>> FindMembershipsForAPIKeyAsync(
        ICallerContext context, string apiKey,
        CancellationToken cancellationToken)
    {
        var keyToken = _tokensService.ParseApiKey(apiKey);
        if (!keyToken.HasValue)
        {
            return Error.EntityNotFound();
        }

        var retrievedApiKey = await _repository.FindByAPIKeyTokenAsync(keyToken.Value.Token, cancellationToken);
        if (!retrievedApiKey.IsSuccessful)
        {
            return retrievedApiKey.Error;
        }

        if (!retrievedApiKey.Value.HasValue)
        {
            return Optional<EndUserWithMemberships>.None;
        }

        var retrievedUser =
            await _endUsersService.GetMembershipsAsync(context, retrievedApiKey.Value.Value.UserId, cancellationToken);
        if (!retrievedUser.IsSuccessful)
        {
            return Optional<EndUserWithMemberships>.None;
        }

        var user = retrievedUser.Value;
        return user.ToOptional();
    }

#if TESTINGONLY
    public async Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext context,
        CancellationToken cancellationToken)
    {
        return await CreateAPIKeyAsync(context, context.CallerId, context.CallerId, null, cancellationToken);
    }
#endif

    public async Task<Result<APIKey, Error>> CreateAPIKeyAsync(ICallerContext context, string userId,
        string description, DateTime? expiresOn, CancellationToken cancellationToken)
    {
        var keyToken = _tokensService.CreateApiKey();

        var created = APIKeyRoot.Create(_recorder, _identifierFactory, _apiKeyHasherService, userId.ToId(), keyToken);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var apiKey = created.Value;
        var parameterized = apiKey.SetParameters(description,
            expiresOn ?? DateTime.UtcNow.ToNearestMinute().Add(DefaultAPIKeyExpiry));
        if (!parameterized.IsSuccessful)
        {
            return parameterized.Error;
        }

        var saved = await _repository.SaveAsync(apiKey, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        return new APIKey
        {
            Description = description,
            ExpiresOnUtc = apiKey.ExpiresOn,
            Key = keyToken.ApiKey,
            UserId = apiKey.UserId,
            Id = apiKey.Id
        };
    }
}