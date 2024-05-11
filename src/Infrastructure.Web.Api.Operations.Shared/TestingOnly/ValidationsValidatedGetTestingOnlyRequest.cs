#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/validations/validated/{Id}", OperationMethod.Get, isTestingOnly: true)]
public class ValidationsValidatedGetTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Id { get; set; }

    public string? OptionalField { get; set; }

    [Required] public string? RequiredField { get; set; }
}
#endif