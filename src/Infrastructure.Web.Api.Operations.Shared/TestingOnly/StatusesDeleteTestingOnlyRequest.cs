#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for DELETE requests, with no response
/// </summary>
[Route("/testingonly/statuses/delete1", OperationMethod.Delete, isTestingOnly: true)]
public class StatusesDeleteTestingOnlyRequest : WebRequestVoid<StatusesDeleteTestingOnlyRequest>;
#endif