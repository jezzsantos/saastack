#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/post", ServiceOperation.Post, isTestingOnly: true)]
public class StatusesPostTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>;
#endif