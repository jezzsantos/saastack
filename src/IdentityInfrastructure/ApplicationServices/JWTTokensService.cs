using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Services.Shared;
using IdentityApplication.ApplicationServices;
using Infrastructure.Common.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace IdentityInfrastructure.ApplicationServices;

public class JWTTokensService : IJWTTokensService
{
    public const string BaseUrlSettingName = "Hosts:IdentityApi:BaseUrl";
    public const string SigningSecretSettingName = "Hosts:IdentityApi:JWT:SigningSecret";
    private const string DefaultExpirySettingName = "Hosts:IdentityApi:JWT:DefaultExpiryInMinutes";
    private readonly TimeSpan _accessTokenExpiresAfter;
    private readonly string _baseUrl;
    private readonly string _signingSecret;
    private readonly ITokensService _tokensService;

    public JWTTokensService(IConfigurationSettings settings, ITokensService tokensService)
    {
        _tokensService = tokensService;
        _signingSecret = settings.Platform.GetString(SigningSecretSettingName);
        _baseUrl = settings.Platform.GetString(BaseUrlSettingName);
        _accessTokenExpiresAfter =
            TimeSpan.FromMinutes(settings.Platform.GetNumber(DefaultExpirySettingName,
                AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalMinutes));
    }

    public Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUserWithMemberships user, UserProfile? profile,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData)
    {
        var tokens = IssueTokens(user, profile, scopes, additionalData);

        return Task.FromResult(tokens);
    }

#if TESTINGONLY
    public static string GenerateSigningKey()
    {
        return RandomNumberGenerator.GetHexString(64);
    }
#endif

    private Result<AccessTokens, Error> IssueTokens(EndUserWithMemberships user, UserProfile? profile,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData)
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var accessTokenExpiresOn = now.Add(_accessTokenExpiresAfter);
        var refreshTokenExpiresOn = now.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var refreshToken = _tokensService.CreateJWTRefreshToken();

        var accessTokenClaims = user.ToClaims(additionalData);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: accessTokenClaims,
            expires: accessTokenExpiresOn,
            signingCredentials: credentials,
            issuer: _baseUrl,
            audience: _baseUrl
        ));

        string? idToken = null;
        DateTime? idTokenExpiresOn = null;
        if (profile.Exists())
        {
            idTokenExpiresOn = now.Add(AuthenticationConstants.Tokens.DefaultIdTokenExpiry);
            var idTokenClaims = profile.ToClaims(scopes, additionalData);
            idToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims: idTokenClaims,
                expires: idTokenExpiresOn,
                signingCredentials: credentials,
                issuer: _baseUrl,
                audience: _baseUrl
            ));
        }

        return new AccessTokens(
            accessToken, accessTokenExpiresOn,
            refreshToken, refreshTokenExpiresOn,
            idToken, idTokenExpiresOn);
    }
}