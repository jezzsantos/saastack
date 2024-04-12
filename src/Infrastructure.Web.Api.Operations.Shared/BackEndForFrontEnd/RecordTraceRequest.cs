using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/trace", OperationMethod.Post)]
public class RecordTraceRequest : UnTenantedEmptyRequest
{
    public List<string>? Arguments { get; set; }

    public required string Level { get; set; }

    public required string MessageTemplate { get; set; }
}