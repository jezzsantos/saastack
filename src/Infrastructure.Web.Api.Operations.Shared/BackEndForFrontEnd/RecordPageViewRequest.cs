using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/page_view", ServiceOperation.Post)]
public class RecordPageViewRequest : UnTenantedEmptyRequest
{
    public required string Path { get; set; }
}