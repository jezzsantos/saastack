#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/validations/unvalidated", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ValidationsUnvalidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif