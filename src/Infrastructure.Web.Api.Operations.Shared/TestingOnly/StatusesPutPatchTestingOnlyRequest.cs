#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for PUT/PATCH requests
/// </summary>
[Route("/testingonly/statuses/putpatch", OperationMethod.PutPatch, isTestingOnly: true)]
public class StatusesPutPatchTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>;
#endif