using System.Security.Cryptography;
using Common;
using Domain.Interfaces.Validations;
using Domain.Services.Shared;
using Domain.Shared.Identities;

namespace Infrastructure.Shared.DomainServices;

public sealed class TokensService : ITokensService
{
    private const int DefaultTokenSizeInBytes = 32;

    public APIKeyToken CreateAPIKey()
    {
        var token = GenerateRandomStringSafeForUrl(CommonValidations.APIKeys.ApiKeyTokenSize,
            CommonValidations.APIKeys.ApiKeyPaddingReplacement);
        var key = GenerateRandomStringSafeForUrl(CommonValidations.APIKeys.ApiKeySize,
            CommonValidations.APIKeys.ApiKeyPaddingReplacement);

        return new APIKeyToken
        {
            Prefix = CommonValidations.APIKeys.ApiKeyPrefix,
            Token = token,
            Key = key,
            ApiKey = $"{CommonValidations.APIKeys.ApiKeyPrefix}{token}{CommonValidations.APIKeys.ApiKeyDelimiter}{key}"
        };
    }

    public string CreateGuestInvitationToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    public string CreateJWTRefreshToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    public string CreateMfaAuthenticationToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    public string CreatePasswordResetToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    public string CreateRegistrationVerificationToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    public string GenerateRandomToken()
    {
        return GenerateRandomStringSafeForUrl();
    }

    /// <summary>
    ///     Should look like this: {ApiKeyPrefix}{token}{ApiKeyDelimiter}{key}
    /// </summary>
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

    private static string GenerateRandomStringSafeForUrl(int keySize = DefaultTokenSizeInBytes,
        string paddingReplacement = "")
    {
        return MakeSafeForUrls(GenerateRandomString(keySize), paddingReplacement);
    }

    private static string GenerateRandomString(int keySize = DefaultTokenSizeInBytes)
    {
        using var random = RandomNumberGenerator.Create();
        var bytes = new byte[keySize];
        random.GetNonZeroBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string MakeSafeForUrls(string value, string paddingReplacement = "")
    {
        return value
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", paddingReplacement);
    }
}