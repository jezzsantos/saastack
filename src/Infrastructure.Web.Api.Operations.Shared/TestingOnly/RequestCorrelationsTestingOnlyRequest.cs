#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/correlations/get", OperationMethod.Get, isTestingOnly: true)]
public class RequestCorrelationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>;
#endif