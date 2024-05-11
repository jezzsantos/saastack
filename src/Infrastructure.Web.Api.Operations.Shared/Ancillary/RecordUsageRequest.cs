using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/record/use", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class RecordUseRequest : UnTenantedEmptyRequest
{
    public Dictionary<string, object?>? Additional { get; set; }

    [Required] public string? EventName { get; set; }
}