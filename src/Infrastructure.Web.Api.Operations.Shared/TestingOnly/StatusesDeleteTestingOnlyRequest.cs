#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/delete", ServiceOperation.Delete, isTestingOnly: true)]
public class StatusesDeleteTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif