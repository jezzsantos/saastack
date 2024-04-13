#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/post1", OperationMethod.Post, isTestingOnly: true)]
public class StatusesPostTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>;
#endif