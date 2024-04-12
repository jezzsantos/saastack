#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/validations/unvalidated", OperationMethod.Get, isTestingOnly: true)]
public class ValidationsUnvalidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif