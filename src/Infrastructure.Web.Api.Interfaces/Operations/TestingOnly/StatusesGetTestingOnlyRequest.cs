#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/statuses/get", ServiceOperation.Get, true)]
[UsedImplicitly]
public class StatusesGetTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif