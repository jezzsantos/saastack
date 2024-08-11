#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for GET requests
/// </summary>
[Route("/testingonly/statuses/get", OperationMethod.Get, isTestingOnly: true)]
public class StatusesGetTestingOnlyRequest : WebRequest<StatusesGetTestingOnlyRequest, StatusesTestingOnlyResponse>;
#endif