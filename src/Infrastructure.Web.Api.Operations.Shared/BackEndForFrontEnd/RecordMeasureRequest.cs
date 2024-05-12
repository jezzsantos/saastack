using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Measures an event in the product
/// </summary>
[Route("/record/measure", OperationMethod.Post)]
public class RecordMeasureRequest : UnTenantedEmptyRequest
{
    public Dictionary<string, object?>? Additional { get; set; }

    [Required] public string? EventName { get; set; }
}