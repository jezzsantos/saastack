using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/page_view", OperationMethod.Post)]
public class RecordPageViewRequest : UnTenantedEmptyRequest
{
    [Required] public string? Path { get; set; }
}