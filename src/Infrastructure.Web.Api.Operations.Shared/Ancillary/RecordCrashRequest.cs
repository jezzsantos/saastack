using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/record/crash", ServiceOperation.Post)]
public class RecordCrashRequest : UnTenantedEmptyRequest
{
    public required string Message { get; set; }
}
