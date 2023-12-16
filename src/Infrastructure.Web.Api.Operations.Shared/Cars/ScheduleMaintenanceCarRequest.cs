using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}/maintain", ServiceOperation.PutPatch)]
public class ScheduleMaintenanceCarRequest : TenantedRequest<GetCarResponse>
{
    public DateTime FromUtc { get; set; }

    public required string Id { get; set; }

    public DateTime ToUtc { get; set; }
}