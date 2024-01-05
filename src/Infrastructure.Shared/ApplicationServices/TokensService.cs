using System.Security.Cryptography;
using Common;
using Domain.Interfaces.Validations;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;

namespace Infrastructure.Shared.ApplicationServices;

public sealed class TokensService : ITokensService
{
    private const int DefaultTokenSizeInBytes = 32;

    public string CreateTokenForPasswordReset()
    {
        return GenerateRandomToken();
    }

    public APIKeyToken CreateApiKey()
    {
        var token = GenerateRandomToken(CommonValidations.APIKeys.ApiKeyTokenSize);
        // ReSharper disable once RedundantArgumentDefaultValue
        var key = GenerateRandomToken(CommonValidations.APIKeys.ApiKeySize);

        return new APIKeyToken
        {
            Prefix = CommonValidations.APIKeys.ApiKeyPrefix,
            Token = token,
            Key = key,
            ApiKey = $"{CommonValidations.APIKeys.ApiKeyPrefix}{token}{CommonValidations.APIKeys.ApiKeyDelimiter}{key}"
        };
    }

    public string CreateTokenForJwtRefresh()
    {
        return GenerateRandomToken();
    }

    public string CreateTokenForVerification()
    {
        return GenerateRandomToken();
    }

    public Optional<APIKeyToken> ParseApiKey(string apiKey)
    {
        if (!CommonValidations.APIKeys.ApiKey.Matches(apiKey))
        {
            return Optional<APIKeyToken>.None;
        }

        var parts = apiKey.Substring(CommonValidations.APIKeys.ApiKeyPrefix.Length)
            .Split(CommonValidations.APIKeys.ApiKeyDelimiter);

        var token = parts[0];
        var key = parts[1];

        return new APIKeyToken
        {
            Prefix = CommonValidations.APIKeys.ApiKeyPrefix,
            Token = token,
            Key = key,
            ApiKey = apiKey
        };
    }

    private static string GenerateRandomToken(int keySize = DefaultTokenSizeInBytes)
    {
        using (var random = RandomNumberGenerator.Create())
        {
            var bytes = new byte[keySize];
            random.GetNonZeroBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}