using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{Id}/release", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ReleaseCarAvailabilityRequest : TenantedRequest<GetCarResponse>
{
    [Required] public DateTime? FromUtc { get; set; }

    [Required] public string? Id { get; set; }

    [Required] public DateTime? ToUtc { get; set; }
}