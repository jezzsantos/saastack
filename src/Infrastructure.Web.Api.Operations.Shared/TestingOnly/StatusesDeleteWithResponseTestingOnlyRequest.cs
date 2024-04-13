#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/delete2", OperationMethod.Delete, isTestingOnly: true)]
public class StatusesDeleteWithResponseTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>;
#endif