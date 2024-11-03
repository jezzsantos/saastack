#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of an empty body in a POST request
/// </summary>
[Route("/testingonly/general/body/empty", OperationMethod.Post, isTestingOnly: true)]
public class
    PostWithEmptyBodyTestingOnlyRequest : WebRequest<PostWithEmptyBodyTestingOnlyRequest,
    StringMessageTestingOnlyResponse>
{
}

#endif