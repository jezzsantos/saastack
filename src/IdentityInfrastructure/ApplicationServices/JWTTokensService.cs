using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Domain.Services.Shared.DomainServices;
using IdentityApplication.ApplicationServices;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.IdentityModel.Tokens;

namespace IdentityInfrastructure.ApplicationServices;

public class JWTTokensService : IJWTTokensService
{
    public const string BaseUrlSettingName = "Hosts:IdentityApi:BaseUrl";
    public const string DefaultExpirySettingName = "Hosts:IdentityApi:JWT:DefaultExpiryInMinutes";
    public const string SecretSettingName = "Hosts:IdentityApi:JWT:SigningSecret";
    private readonly string _baseUrl;
    private readonly TimeSpan _expiresAfter;
    private readonly string _signingSecret;
    private readonly ITokensService _tokensService;

    public JWTTokensService(IConfigurationSettings settings, ITokensService tokensService)
    {
        _tokensService = tokensService;
        _signingSecret = settings.Platform.GetString(SecretSettingName);
        _baseUrl = settings.Platform.GetString(BaseUrlSettingName);
        _expiresAfter =
            TimeSpan.FromMinutes(settings.Platform.GetNumber(DefaultExpirySettingName,
                AuthenticationConstants.DefaultAccessTokenExpiry.TotalMinutes));
    }

    public Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUser user)
    {
        var tokens = IssueTokens(user);

        return Task.FromResult(tokens);
    }

    private Result<AccessTokens, Error> IssueTokens(EndUser user)
    {
        var expiresOn = DateTime.UtcNow.Add(_expiresAfter);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var claims = new List<Claim>
        {
            new(AuthenticationConstants.ClaimForId, user.Id)
        };
        user.Roles.ForEach(role => claims.Add(new Claim(AuthenticationConstants.ClaimForRole, role)));
        user.FeatureLevels.ForEach(feature =>
            claims.Add(new Claim(AuthenticationConstants.ClaimForFeatureLevel, feature)));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresOn,
            signingCredentials: credentials,
            issuer: _baseUrl,
            audience: _baseUrl
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = _tokensService.CreateTokenForJwtRefresh();

        return new AccessTokens(accessToken, refreshToken, expiresOn);
    }
}