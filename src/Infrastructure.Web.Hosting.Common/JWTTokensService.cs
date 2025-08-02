using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Services.Shared;
using Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a service for issuing JWT tokens. That are both issued by and verified by the host where this code is
///     running. Tokens are signed with RSA256 asymmetric algorithm, so that we can support OpenIdConnect,
///     where some of those issued tokens may need to be verified by 3rd parties.
///     Tokens are issued with a short lifetime (15 minutes default), and a long-lived refresh token (7 days default).
/// </summary>
public sealed class JWTTokensService : IJWTTokensService
{
    public const string BaseUrlSettingName = "Hosts:IdentityApi:BaseUrl";
    public const string SigningPrivateKeySettingName = "Hosts:IdentityApi:JWT:PrivateKey";
    public const string SigningPublicKeySettingName = "Hosts:IdentityApi:JWT:PublicKey";
    private const string DefaultExpirySettingName = "Hosts:IdentityApi:JWT:DefaultExpiryInMinutes";
    private readonly TimeSpan _accessTokenExpiresAfter;
    private readonly string _baseUrl;
    private readonly RSA _signingPrivateKey;
    private readonly ITokensService _tokensService;

    public JWTTokensService(IConfigurationSettings settings, ITokensService tokensService)
    {
        _tokensService = tokensService;
        _baseUrl = settings.Platform.GetString(BaseUrlSettingName);
        _accessTokenExpiresAfter =
            TimeSpan.FromMinutes(settings.Platform.GetNumber(DefaultExpirySettingName,
                AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalMinutes));
        _signingPrivateKey = RSA.Create();
        _signingPrivateKey.ImportFromPem(settings.Platform.GetString(SigningPrivateKeySettingName));
    }

    public Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUserWithMemberships user, UserProfile? profile,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData)
    {
        var tokens = IssueTokens(user, profile, scopes, additionalData);

        return Task.FromResult(tokens);
    }

#if TESTINGONLY
    public static (string privateKey, string publicKey) GenerateSigningKey()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();
        return (privateKey, publicKey);
    }
#endif

    /// <summary>
    ///     Returns the token validation parameters used to validate inbound tokens.
    /// </summary>
    public static TokenValidationParameters GetTokenValidationParameters(IConfiguration configuration)
    {
        var signingPublicKey = RSA.Create();
        signingPublicKey.ImportFromPem(configuration[SigningPublicKeySettingName]);

        return new TokenValidationParameters
        {
            RoleClaimType = AuthenticationConstants.Claims.ForRole,
            NameClaimType = AuthenticationConstants.Claims.ForId,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidAudience = configuration[BaseUrlSettingName],
            ValidIssuer = configuration[BaseUrlSettingName],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(signingPublicKey)
        };
    }

    private Result<AccessTokens, Error> IssueTokens(EndUserWithMemberships user, UserProfile? profile,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData)
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var accessTokenExpiresOn = now.Add(_accessTokenExpiresAfter);
        var refreshTokenExpiresOn = now.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry);

        var key = new RsaSecurityKey(_signingPrivateKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var refreshToken = _tokensService.CreateJWTRefreshToken();
        var accessTokenClaims = user.ToClaims(scopes, additionalData);
        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(
                new JwtSecurityToken(
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
            if (!(additionalData ?? new Dictionary<string, object>())
                .TryGetValue(AuthenticationConstants.Claims.ForClientId, out var clientId))
            {
                return Error.PreconditionViolation(Resources.JWTTokensService_ClientIdRequired);
            }

            idTokenExpiresOn = now.Add(AuthenticationConstants.Tokens.DefaultIdTokenExpiry);
            var idTokenClaims = profile.ToClaims(scopes, additionalData);
            idToken = new JwtSecurityTokenHandler()
                .WriteToken(
                    new JwtSecurityToken(
                        claims: idTokenClaims,
                        expires: idTokenExpiresOn,
                        signingCredentials: credentials,
                        issuer: _baseUrl,
                        audience: clientId.ToString()
                    ));
        }

        return new AccessTokens(
            accessToken, accessTokenExpiresOn,
            refreshToken, refreshTokenExpiresOn,
            idToken, idTokenExpiresOn);
    }
}