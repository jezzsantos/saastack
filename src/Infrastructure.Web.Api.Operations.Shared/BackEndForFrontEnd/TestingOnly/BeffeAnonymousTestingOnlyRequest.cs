#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;

/// <summary>
///     An example of a request that uses NO authentication for a BEFFE endpoint
/// </summary>
[Route("/testingonly/anonymous", OperationMethod.Post, isTestingOnly: true)]
public class
    BeffeAnonymousTestingOnlyRequest : UnTenantedRequest<BeffeAnonymousTestingOnlyRequest, BeffeTestingOnlyResponse>
{
}
#endif