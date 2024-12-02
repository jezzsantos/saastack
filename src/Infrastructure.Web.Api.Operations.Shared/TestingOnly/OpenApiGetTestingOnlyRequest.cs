#if TESTINGONLY
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests OpenAPI swagger for GET requests
///     This includes multiple lines explaining things
/// </summary>
/// <remarks>
///     This is some explanation
/// </remarks>
[Route("/testingonly/openapi/{Id}", OperationMethod.Get, isTestingOnly: true)]
[UsedImplicitly]
public class OpenApiGetTestingOnlyRequest : WebRequest<OpenApiGetTestingOnlyRequest, OpenApiTestingOnlyResponse>
{
    [Description("anid")] public string? Id { get; set; }

    [Description("anoptionalfield")] public string? OptionalField { get; set; }

    [Description("arequiredfield")]
    [Required]
    public string? RequiredField { get; set; }
}
#endif