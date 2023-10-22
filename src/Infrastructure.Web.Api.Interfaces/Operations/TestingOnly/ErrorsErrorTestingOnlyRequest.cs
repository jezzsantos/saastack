#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/errors/error", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ErrorsErrorTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif