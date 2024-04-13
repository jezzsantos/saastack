#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/delete1", OperationMethod.Delete, isTestingOnly: true)]
public class StatusesDeleteTestingOnlyRequest : IWebRequest;
#endif