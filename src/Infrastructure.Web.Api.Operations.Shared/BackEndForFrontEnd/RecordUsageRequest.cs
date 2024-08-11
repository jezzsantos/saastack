using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Records a usage of the product
/// </summary>
[Route("/record/use", OperationMethod.Post)]
public class RecordUseRequest : UnTenantedEmptyRequest<RecordUseRequest>
{
    public Dictionary<string, object?>? Additional { get; set; }

    [Required] public string? EventName { get; set; }
}