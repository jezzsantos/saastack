#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests OpenAPI swagger for multipart-form POST requests
/// </summary>
[Route("/testingonly/openapi/{Id}/binary", OperationMethod.Post, isTestingOnly: true)]
[UsedImplicitly]
public class OpenApiPostMultiPartFormTestingOnlyRequest :
    WebRequest<OpenApiPostMultiPartFormTestingOnlyRequest, StringMessageTestingOnlyResponse>,
    IHasMultipartForm
{
    public string? Id { get; set; }

    public string? OptionalField { get; set; }

    [Required] public string? RequiredField { get; set; }
}
#endif