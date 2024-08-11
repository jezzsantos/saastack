#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests errors, by throwing an exception
/// </summary>
[Route("/testingonly/errors/throws", OperationMethod.Get, isTestingOnly: true)]
public class
    ErrorsThrowTestingOnlyRequest : WebRequest<ErrorsThrowTestingOnlyRequest, StringMessageTestingOnlyResponse>;
#endif