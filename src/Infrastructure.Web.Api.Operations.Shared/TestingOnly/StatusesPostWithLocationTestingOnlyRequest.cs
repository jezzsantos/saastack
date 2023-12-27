using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/post2", ServiceOperation.Post, isTestingOnly: true)]
[UsedImplicitly]
public class StatusesPostWithLocationTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif