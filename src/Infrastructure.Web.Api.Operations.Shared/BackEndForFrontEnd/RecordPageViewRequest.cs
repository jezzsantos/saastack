using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Records a page view event in the product
/// </summary>
[Route("/record/page_view", OperationMethod.Post)]
public class RecordPageViewRequest : UnTenantedEmptyRequest<RecordPageViewRequest>
{
    [Required] public string? Path { get; set; }
}