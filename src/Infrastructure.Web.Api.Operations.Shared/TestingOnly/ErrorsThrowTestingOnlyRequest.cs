#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/errors/throws", ServiceOperation.Get, isTestingOnly: true)]
public class ErrorsThrowTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif