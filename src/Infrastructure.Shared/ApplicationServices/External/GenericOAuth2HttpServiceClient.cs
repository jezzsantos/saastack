using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides a general purpose generic OAuth2 service client for exchanging authorization codes for tokens.
///     Assumes <see href="https://datatracker.ietf.org/doc/html/rfc6749">The OAuth 2.0 Authorization Framework</see>
/// </summary>
public class GenericOAuth2HttpServiceClient : IOAuth2Service
{
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly IRecorder _recorder;
    private readonly string _redirectUri;
    private readonly IServiceClient _serviceClient;

    public GenericOAuth2HttpServiceClient(IRecorder recorder, IServiceClient serviceClient, string clientId,
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
            //We want you to be able to override this and use any IOAuth2GrantAuthorizationRequest
            var response = await _serviceClient.PostAsync(caller, new GenericOAuth2GrantAuthorizationRequest
            {
                GrantType = "authorization_code",
                Code = options.Code,
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                Scope = options.Scope,
                RedirectUri = _redirectUri
            }, null, cancellationToken);

            if (response.IsFailure)
            {
                return Error.NotAuthenticated(response.Error.Detail ?? response.Error.Title);
            }

            return response.Value.ToTokens();
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
            var response = await _serviceClient.PostAsync(caller, new GenericOAuth2GrantAuthorizationRequest
            {
                GrantType = "refresh_token",
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                RefreshToken = options.RefreshToken
            }, null, cancellationToken);

            if (response.IsFailure)
            {
                return Error.NotAuthenticated(response.Error.Detail ?? response.Error.Title);
            }

            return response.Value.ToTokens();
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

internal static class OAuth2ConversionExtensions
{
    public static List<AuthToken> ToTokens(this GenericOAuth2GrantAuthorizationResponse response)
    {
        var tokens = new List<AuthToken>();
        var now = DateTime.UtcNow.ToNearestSecond();
        var expiresOn = now.Add(TimeSpan.FromSeconds(response.ExpiresIn));
        tokens.Add(new AuthToken(TokenType.AccessToken, response.AccessToken, expiresOn));
        if (response.RefreshToken.HasValue())
        {
            // Note: Refresh tokens are typically long-lived, like: for days or weeks
            var defaultRefreshTokenExpiry = TimeSpan.FromDays(1); //default from Microsoft Identity (for SPA)
            var refreshExpiresOn = now.Add(defaultRefreshTokenExpiry);
            tokens.Add(new AuthToken(TokenType.RefreshToken, response.RefreshToken!, refreshExpiresOn));
        }

        if (response.IdToken.HasValue())
        {
            // Note: ID tokens are typically very short-lived, like: less than an hour or so
            var defaultIdTokenExpiry = TimeSpan.FromHours(1); //default from Microsoft Identity
            var idTokenExpiresOn = now.Add(defaultIdTokenExpiry);
            tokens.Add(new AuthToken(TokenType.OtherToken, response.IdToken, idTokenExpiresOn));
        }

        return tokens;
    }
}