using System.Text.Json;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Common.Clients;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides a <see cref="IOAuth2Service" /> for accessing Microsoft Identity platform
///     <see
///         href="https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow#redeem-a-code-for-an-access-token" />
/// </summary>
public class MicrosoftOAuth2HttpServiceClient : IOAuth2Service
{
    private const string BaseUrlSettingName = "ApplicationServices:MicrosoftIdentity:BaseUrl";
    private const string ClientIdSettingName = "ApplicationServices:MicrosoftIdentity:ClientId";
    private const string ClientSecretSettingName = "ApplicationServices:MicrosoftIdentity:ClientSecret";
    private const string RedirectUriSettingName = "ApplicationServices:MicrosoftIdentity:RedirectUri";
    private readonly IOAuth2Service _oauth2Service;

    public MicrosoftOAuth2HttpServiceClient(IRecorder recorder, IHttpClientFactory clientFactory,
        JsonSerializerOptions jsonOptions, IConfigurationSettings settings) : this(new GenericOAuth2HttpServiceClient(
        recorder, new ApiServiceClient(clientFactory, jsonOptions, settings.GetString(BaseUrlSettingName)),
        settings.GetString(ClientIdSettingName), settings.GetString(ClientSecretSettingName),
        settings.GetString(RedirectUriSettingName)))
    {
    }

    internal MicrosoftOAuth2HttpServiceClient(IOAuth2Service oauth2Service)
    {
        _oauth2Service = oauth2Service;
    }

    public async Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2CodeTokenExchangeOptions options, CancellationToken cancellationToken)
    {
        return await _oauth2Service.ExchangeCodeForTokensAsync(caller, options, cancellationToken);
    }

    public async Task<Result<List<AuthToken>, Error>> RefreshTokenAsync(ICallerContext caller,
        OAuth2RefreshTokenOptions options, CancellationToken cancellationToken)
    {
        return await _oauth2Service.RefreshTokenAsync(caller, options, cancellationToken);
    }
}