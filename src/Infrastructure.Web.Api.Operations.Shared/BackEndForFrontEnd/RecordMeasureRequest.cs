using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/measure", OperationMethod.Post)]
public class RecordMeasureRequest : UnTenantedEmptyRequest
{
    public Dictionary<string, object?>? Additional { get; set; }

    public required string EventName { get; set; }
}