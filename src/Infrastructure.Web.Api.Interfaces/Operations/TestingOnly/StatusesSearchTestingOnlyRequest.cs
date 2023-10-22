#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/statuses/search", ServiceOperation.Search, true)]
[UsedImplicitly]
public class StatusesSearchTestingOnlyRequest : IWebRequest<StatusesTestingOnlySearchResponse>
{
}
#endif