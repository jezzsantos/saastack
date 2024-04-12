#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/post2", OperationMethod.Post, isTestingOnly: true)]
public class StatusesPostWithLocationTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>;
#endif