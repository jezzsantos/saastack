using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Measures an event in the product
/// </summary>
[Route("/record/measure", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class RecordMeasureRequest : UnTenantedEmptyRequest<RecordMeasureRequest>
{
    public Dictionary<string, object?>? Additional { get; set; }

    [Required] public string? EventName { get; set; }
}