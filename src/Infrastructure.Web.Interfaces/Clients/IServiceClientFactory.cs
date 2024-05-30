namespace Infrastructure.Web.Interfaces.Clients;

/// <summary>
///     Defines a factory for creating service clients for calling remote APIs
/// </summary>
public interface IServiceClientFactory
{
    /// <summary>
    ///     Creates a service client for calling remote APIs
    /// </summary>
    IServiceClient CreateServiceClient(string baseUrl);
}