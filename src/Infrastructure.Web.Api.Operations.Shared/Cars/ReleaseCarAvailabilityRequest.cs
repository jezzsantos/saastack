using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}/release", ServiceOperation.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ReleaseCarAvailabilityRequest : TenantedRequest<GetCarResponse>
{
    public required DateTime FromUtc { get; set; }

    public required string Id { get; set; }

    public required DateTime ToUtc { get; set; }
}