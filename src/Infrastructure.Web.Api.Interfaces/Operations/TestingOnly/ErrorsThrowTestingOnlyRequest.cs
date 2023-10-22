#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/errors/throws", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ErrorsThrowTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif