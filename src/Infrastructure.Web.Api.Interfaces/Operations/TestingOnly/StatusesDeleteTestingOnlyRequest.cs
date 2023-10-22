#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/statuses/delete", ServiceOperation.Delete, true)]
[UsedImplicitly]
public class StatusesDeleteTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif