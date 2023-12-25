using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/record/trace", ServiceOperation.Post)]
public class RecordTraceRequest : UnTenantedEmptyRequest
{
    public List<string>? Arguments { get; set; }

    public required string Level { get; set; }

    public required string MessageTemplate { get; set; }
}