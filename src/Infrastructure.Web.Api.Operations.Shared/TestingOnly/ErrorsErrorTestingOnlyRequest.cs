#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/errors/error", OperationMethod.Get, isTestingOnly: true)]
public class ErrorsErrorTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>;
#endif