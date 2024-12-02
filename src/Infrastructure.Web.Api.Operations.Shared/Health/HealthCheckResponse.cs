using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Health;

public class HealthCheckResponse : IWebResponse
{
    public required string Name { get; set; }

    public required string Status { get; set; }
}