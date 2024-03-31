using System.Security.Cryptography;
using Common;
using Domain.Interfaces.Validations;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;

namespace Infrastructure.Shared.DomainServices;

public sealed class TokensService : ITokensService
{
    private const int DefaultTokenSizeInBytes = 32;

    public APIKeyToken CreateAPIKey()
    {
        var token = GenerateRandomTokenSafeForUrl(CommonValidations.APIKeys.ApiKeyTokenSize,
            CommonValidations.APIKeys.ApiKeyPaddingReplacement);
        var key = GenerateRandomTokenSafeForUrl(CommonValidations.APIKeys.ApiKeySize,
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
        return GenerateRandomTokenSafeForUrl();
    }

    public string CreateJWTRefreshToken()
    {
        return GenerateRandomTokenSafeForUrl();
    }

    public string CreatePasswordResetToken()
    {
        return GenerateRandomTokenSafeForUrl();
    }

    public string CreateRegistrationVerificationToken()
    {
        return GenerateRandomTokenSafeForUrl();
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

    private static string GenerateRandomTokenSafeForUrl(int keySize = DefaultTokenSizeInBytes,
        string paddingReplacement = "")
    {
        return MakeSafeForUrls(GenerateRandomToken(keySize), paddingReplacement);
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

    private static string MakeSafeForUrls(string value, string paddingReplacement = "")
    {
        return value
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", paddingReplacement);
    }
}