using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}/reserve", ServiceOperation.PutPatch)]
public class ReserveCarIfAvailableRequest : TenantedRequest<ReserveCarIfAvailableResponse>
{
    public required DateTime FromUtc { get; set; }

    public required string Id { get; set; }

    public required string ReferenceId { get; set; }

    public required DateTime ToUtc { get; set; }
}