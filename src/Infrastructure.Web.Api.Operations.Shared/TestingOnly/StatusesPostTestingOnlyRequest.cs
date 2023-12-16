using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/post", ServiceOperation.Post, true)]
[UsedImplicitly]
public class StatusesPostTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif