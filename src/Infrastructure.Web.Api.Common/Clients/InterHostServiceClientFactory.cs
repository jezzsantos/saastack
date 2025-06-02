using System.Text.Json;
using Application.Interfaces.Services;
using Infrastructure.Web.Api.Interfaces.Clients;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     A factory of <see cref="InterHostServiceClient" /> connected to a remote host.
/// </summary>
public class InterHostServiceClientFactory : IServiceClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _privateInterHostSecret;
    private readonly string _hmacSecret;

    public InterHostServiceClientFactory(IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonSerializerOptions, IHostSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
        _privateInterHostSecret = settings.GetPrivateInterHostHmacAuthSecret();
        _hmacSecret = settings.GetAncillaryApiHostHmacAuthSecret();
    }

    public IServiceClient CreateServiceClient(string baseUrl)
    {
        return new InterHostServiceClient(_httpClientFactory, _jsonSerializerOptions, baseUrl, _privateInterHostSecret,
            _hmacSecret);
    }
}