#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests errors, by returning an error result
/// </summary>
[Route("/testingonly/errors/error", OperationMethod.Get, isTestingOnly: true)]
public class ErrorsErrorTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>;
#endif