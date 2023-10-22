#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

[Route("/testingonly/statuses/post", ServiceOperation.Post, true)]
[UsedImplicitly]
public class StatusesPostTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif