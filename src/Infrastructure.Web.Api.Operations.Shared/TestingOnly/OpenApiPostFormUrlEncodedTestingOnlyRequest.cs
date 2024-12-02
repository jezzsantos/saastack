#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests OpenAPI swagger for application/x-www-form-urlencoded POST requests
/// </summary>
[Route("/testingonly/openapi/{Id}/urlencoded", OperationMethod.Post, isTestingOnly: true)]
[UsedImplicitly]
public class OpenApiPostFormUrlEncodedTestingOnlyRequest :
    WebRequest<OpenApiPostFormUrlEncodedTestingOnlyRequest, OpenApiTestingOnlyResponse>,
    IHasFormUrlEncoded
{
    public string? Id { get; set; }

    public string? OptionalField { get; set; }

    [Required] public string? RequiredField { get; set; }
}
#endif