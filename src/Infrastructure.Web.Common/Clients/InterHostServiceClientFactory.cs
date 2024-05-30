using System.Text.Json;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     A factory of <see cref="InterHostServiceClient" /> connected to a remote host.
/// </summary>
public class InterHostServiceClientFactory : IServiceClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public InterHostServiceClientFactory(IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _httpClientFactory = httpClientFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public IServiceClient CreateServiceClient(string baseUrl)
    {
        return new InterHostServiceClient(_httpClientFactory, _jsonSerializerOptions, baseUrl);
    }
}