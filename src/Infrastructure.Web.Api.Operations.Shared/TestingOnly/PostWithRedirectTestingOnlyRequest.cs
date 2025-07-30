#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of a redirect for a POST method
/// </summary>
[Route("/testingonly/redirect/post", OperationMethod.Post, isTestingOnly: true)]
public class
    PostWithRedirectTestingOnlyRequest : WebRequest<PostWithRedirectTestingOnlyRequest,
    StringMessageTestingOnlyResponse>
{
    public string? Result { get; set; }
}
#endif