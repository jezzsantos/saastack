using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Domain.Services.Shared;
using IdentityApplication.ApplicationServices;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
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

    public Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUserWithMemberships user)
    {
        var tokens = IssueTokens(user);

        return Task.FromResult(tokens);
    }

#if TESTINGONLY
    public static string GenerateSigningKey()
    {
        return RandomNumberGenerator.GetHexString(64);
    }
#endif

    private Result<AccessTokens, Error> IssueTokens(EndUserWithMemberships user)
    {
        var accessTokenExpiresOn = DateTime.UtcNow.Add(_accessTokenExpiresAfter);
        var refreshTokenExpiresOn = DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var claims = user.ToClaims();

        var token = new JwtSecurityToken(
            claims: claims,
            expires: accessTokenExpiresOn,
            signingCredentials: credentials,
            issuer: _baseUrl,
            audience: _baseUrl
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = _tokensService.CreateJWTRefreshToken();

        return new AccessTokens(accessToken, accessTokenExpiresOn, refreshToken, refreshTokenExpiresOn);
    }
}