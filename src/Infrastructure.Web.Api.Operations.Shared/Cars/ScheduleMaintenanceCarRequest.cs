using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Schedules the car for maintenance for the specified period
/// </summary>
[Route("/cars/{Id}/maintain", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ScheduleMaintenanceCarRequest : TenantedRequest<ScheduleMaintenanceCarRequest, GetCarResponse>
{
    public DateTime FromUtc { get; set; }

    [Required] public string? Id { get; set; }

    public DateTime ToUtc { get; set; }
}