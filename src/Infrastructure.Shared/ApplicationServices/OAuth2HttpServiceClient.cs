using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a general purpose OAuth2 service client for exchanging authorization codes for tokens.
///     Assumes <see href="https://datatracker.ietf.org/doc/html/rfc6749">The OAuth 2.0 Authorization Framework</see>
/// </summary>
public class OAuth2HttpServiceClient : IOAuth2Service
{
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly IRecorder _recorder;
    private readonly string _redirectUri;
    private readonly IServiceClient _serviceClient;

    public OAuth2HttpServiceClient(IRecorder recorder, IServiceClient serviceClient, string clientId,
        string? clientSecret, string redirectUri)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _redirectUri = redirectUri;
    }

    public async Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2CodeTokenExchangeOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceClient.PostAsync(caller, new OAuth2GrantAuthorizationRequest
            {
                GrantType = "authorization_code",
                Code = options.Code,
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                Scope = options.Scope,
                RedirectUri = _redirectUri
            }, null, cancellationToken);

            var tokens = new List<AuthToken>();
            if (response.IsFailure)
            {
                return Error.NotAuthenticated(response.Error.Detail ?? response.Error.Title);
            }

            var expiresOn = DateTime.UtcNow.Add(TimeSpan.FromSeconds(response.Value.ExpiresIn));
            tokens.Add(new AuthToken(TokenType.AccessToken, response.Value.AccessToken!, expiresOn));
            if (response.Value.RefreshToken.HasValue())
            {
                tokens.Add(new AuthToken(TokenType.RefreshToken, response.Value.RefreshToken!, null));
            }

            return tokens;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(caller.ToCall(), ex, "Failed to exchange OAuth2 code with OAuth2 server {Server}",
                options.ServiceName);
            return Error.Unexpected(ex.Message);
        }
    }

    public async Task<Result<List<AuthToken>, Error>> RefreshTokenAsync(ICallerContext caller,
        OAuth2RefreshTokenOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceClient.PostAsync(caller, new OAuth2GrantAuthorizationRequest
            {
                GrantType = "refresh_token",
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                RefreshToken = options.RefreshToken
            }, null, cancellationToken);

            var tokens = new List<AuthToken>();
            if (response.IsFailure)
            {
                return Error.NotAuthenticated(response.Error.Detail ?? response.Error.Title);
            }

            var expiresOn = DateTime.UtcNow.Add(TimeSpan.FromSeconds(response.Value.ExpiresIn));
            tokens.Add(new AuthToken(TokenType.AccessToken, response.Value.AccessToken!, expiresOn));
            if (response.Value.RefreshToken.HasValue())
            {
                tokens.Add(new AuthToken(TokenType.RefreshToken, response.Value.RefreshToken!, null));
            }

            return tokens;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(caller.ToCall(), ex,
                "Failed to refresh OAuth2 refresh token with OAuth2 server {Server}",
                options.ServiceName);
            return Error.Unexpected(ex.Message);
        }
    }
}