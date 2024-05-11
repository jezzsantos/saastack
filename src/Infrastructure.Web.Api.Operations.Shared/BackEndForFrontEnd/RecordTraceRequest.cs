using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/trace", OperationMethod.Post)]
public class RecordTraceRequest : UnTenantedEmptyRequest
{
    public List<string>? Arguments { get; set; }

    [Required] public string? Level { get; set; }

    [Required] public string? MessageTemplate { get; set; }
}