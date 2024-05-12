#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests response for validated POST requests
/// </summary>
[Route("/testingonly/validations/validated/{Id}", OperationMethod.Post, isTestingOnly: true)]
public class ValidationsValidatedPostTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Id { get; set; }

    public string? OptionalField { get; set; }

    [Required] public string? RequiredField { get; set; }
}
#endif