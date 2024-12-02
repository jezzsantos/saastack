namespace IntegrationTesting.Website.Common.Stubs;

public class StubHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:5001");
        return client;
    }
}