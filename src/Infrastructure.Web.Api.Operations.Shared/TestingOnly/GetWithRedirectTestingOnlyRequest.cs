#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of a redirect for a GET method
/// </summary>
[Route("/testingonly/redirect/get", OperationMethod.Get, isTestingOnly: true)]
public class
    GetWithRedirectTestingOnlyRequest : WebRequest<GetWithRedirectTestingOnlyRequest, StringMessageTestingOnlyResponse>
{
    public string? Result { get; set; }
}
#endif