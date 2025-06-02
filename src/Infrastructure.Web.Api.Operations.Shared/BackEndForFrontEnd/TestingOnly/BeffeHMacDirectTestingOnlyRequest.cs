#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;

/// <summary>
///     An example of a request that uses HMAC authentication for a BEFFE endpoint,
///     which can be called directly from another client (and not from the browser, which includes CSRF protection).
/// </summary>
[Route("/testingonly/direct/hmac", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class
    BeffeHMacDirectTestingOnlyRequest : UnTenantedRequest<BeffeHMacDirectTestingOnlyRequest, BeffeTestingOnlyResponse>
{
}
#endif