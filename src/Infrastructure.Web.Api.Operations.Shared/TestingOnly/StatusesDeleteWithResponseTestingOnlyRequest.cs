#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for DELETE requests, with a response
/// </summary>
[Route("/testingonly/statuses/delete2", OperationMethod.Delete, isTestingOnly: true)]
public class
    StatusesDeleteWithResponseTestingOnlyRequest : WebRequest<StatusesDeleteWithResponseTestingOnlyRequest,
    StatusesTestingOnlyResponse>;
#endif