using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Worker.Api.IntegrationTests.Stubs;

public class StubServiceClientFactory : IServiceClientFactory
{
    private readonly StubServiceClient _serviceClient = new();

    public Optional<IWebRequest> LastPostedMessage => _serviceClient.LastPostedMessage;

    public IServiceClient CreateServiceClient(string baseUrl)
    {
        return _serviceClient;
    }

    public void Reset()
    {
        _serviceClient.Reset();
    }
}