#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/search", OperationMethod.Search, isTestingOnly: true)]
public class StatusesSearchTestingOnlyRequest : IWebRequest<StatusesTestingOnlySearchResponse>;
#endif