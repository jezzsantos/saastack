using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Health;

public class HealthCheckResponse : IWebResponse
{
    public string? Name { get; set; }

    public string? Status { get; set; }
}