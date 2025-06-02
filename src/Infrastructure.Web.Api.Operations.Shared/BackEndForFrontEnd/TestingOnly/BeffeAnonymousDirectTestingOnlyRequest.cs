#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;

/// <summary>
///     An example of a request that uses NO authentication for a BEFFE endpoint,
///     which can be called directly from another client (and not from the browser, which includes CSRF protection).
/// </summary>
[Route("/testingonly/direct/anonymous", OperationMethod.Post, isTestingOnly: true)]
public class
    BeffeAnonymousDirectTestingOnlyRequest : UnTenantedRequest<BeffeAnonymousDirectTestingOnlyRequest,
    BeffeTestingOnlyResponse>
{
}
#endif