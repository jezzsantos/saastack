#if TESTINGONLY
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests OpenAPI swagger for POST requests
/// </summary>
/// <response code="409">a custom conflict response</response>
/// <response code="419">a special response</response>
[Route("/testingonly/openapi/{Id}", OperationMethod.PutPatch, isTestingOnly: true)]
[UsedImplicitly]
public class OpenApiPutTestingOnlyRequest : WebRequest<OpenApiPutTestingOnlyRequest, OpenApiTestingOnlyResponse>
{
    [Description("anid")] public string? Id { get; set; }

    [Description("anoptionalfield")] public string? OptionalField { get; set; }

    [Description("arequiredfield")]
    [Required]
    public string? RequiredField { get; set; }
}
#endif