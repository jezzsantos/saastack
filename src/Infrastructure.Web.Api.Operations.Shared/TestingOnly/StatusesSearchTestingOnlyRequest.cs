using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/search", ServiceOperation.Search, isTestingOnly: true)]
[UsedImplicitly]
public class StatusesSearchTestingOnlyRequest : IWebRequest<StatusesTestingOnlySearchResponse>
{
}
#endif