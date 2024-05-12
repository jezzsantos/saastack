using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Health;

/// <summary>
///     Displays the health of the API
/// </summary>
[Route("/health", OperationMethod.Get)]
public class HealthCheckRequest : UnTenantedRequest<HealthCheckResponse>;