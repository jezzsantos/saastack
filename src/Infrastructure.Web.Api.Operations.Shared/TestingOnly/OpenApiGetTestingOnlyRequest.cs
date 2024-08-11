#if TESTINGONLY
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests OpenAPI swagger for GET requests
/// </summary>
[Route("/testingonly/openapi/{Id}", OperationMethod.Get, isTestingOnly: true)]
[UsedImplicitly]
public class OpenApiGetTestingOnlyRequest : WebRequest<OpenApiGetTestingOnlyRequest, StringMessageTestingOnlyResponse>
{
    [Description("anid")] public string? Id { get; set; }

    [Description("anoptionalfield")] public string? OptionalField { get; set; }

    [Description("arequiredfield")]
    [Required]
    public string? RequiredField { get; set; }
}
#endif