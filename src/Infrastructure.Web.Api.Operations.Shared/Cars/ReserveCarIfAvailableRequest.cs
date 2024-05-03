using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{Id}/reserve", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ReserveCarIfAvailableRequest : TenantedRequest<ReserveCarIfAvailableResponse>
{
    public required DateTime FromUtc { get; set; }

    public required string Id { get; set; }

    public required string ReferenceId { get; set; }

    public required DateTime ToUtc { get; set; }
}