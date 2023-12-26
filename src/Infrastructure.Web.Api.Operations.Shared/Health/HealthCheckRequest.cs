using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Health;

[Route("/health", ServiceOperation.Get)]
public class HealthCheckRequest : UnTenantedRequest<HealthCheckResponse>
{
}