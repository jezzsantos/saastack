using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/record/measure", ServiceOperation.Post, AccessType.HMAC)]
public class RecordMeasureRequest : UnTenantedEmptyRequest
{
    public Dictionary<string, object?>? Additional { get; set; }

    public required string EventName { get; set; }
}