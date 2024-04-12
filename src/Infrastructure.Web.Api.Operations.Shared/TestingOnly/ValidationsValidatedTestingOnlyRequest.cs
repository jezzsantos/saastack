#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/validations/validated/{id}", OperationMethod.Get, isTestingOnly: true)]
public class ValidationsValidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Field1 { get; set; }

    public string? Field2 { get; set; }

    public string? Id { get; set; }
}
#endif