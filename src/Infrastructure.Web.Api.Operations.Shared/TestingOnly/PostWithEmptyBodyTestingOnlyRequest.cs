#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of an empty post body
/// </summary>
[Route("/testingonly/general/body/empty", OperationMethod.Post, isTestingOnly: true)]
public class
    PostWithEmptyBodyTestingOnlyRequest : WebRequest<PostWithEmptyBodyTestingOnlyRequest,
    StringMessageTestingOnlyResponse>
{
}

#endif