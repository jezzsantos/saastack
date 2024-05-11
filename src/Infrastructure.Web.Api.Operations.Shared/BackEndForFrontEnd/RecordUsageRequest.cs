using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/use", OperationMethod.Post)]
public class RecordUseRequest : UnTenantedEmptyRequest
{
    public Dictionary<string, object?>? Additional { get; set; }

    [Required] public string? EventName { get; set; }
}