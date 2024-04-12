using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Health;

[Route("/health", OperationMethod.Get)]
public class HealthCheckRequest : UnTenantedRequest<HealthCheckResponse>;