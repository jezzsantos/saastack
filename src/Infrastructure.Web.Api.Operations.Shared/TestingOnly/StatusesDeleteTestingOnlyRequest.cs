using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/delete", ServiceOperation.Delete, isTestingOnly: true)]
[UsedImplicitly]
public class StatusesDeleteTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif