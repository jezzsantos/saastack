using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Records a crash event of the product
/// </summary>
[Route("/record/crash", OperationMethod.Post)]
public class RecordCrashRequest : UnTenantedEmptyRequest<RecordCrashRequest>
{
    [Required] public string? Message { get; set; }
}