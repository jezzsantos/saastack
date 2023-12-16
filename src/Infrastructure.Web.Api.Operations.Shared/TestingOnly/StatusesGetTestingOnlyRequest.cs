using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/get", ServiceOperation.Get, true)]
[UsedImplicitly]
public class StatusesGetTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif