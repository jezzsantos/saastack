#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with Inter Host signature authentication
/// </summary>
[Route("/testingonly/authn/interhost/get", OperationMethod.Get, AccessType.PrivateInterHost, true)]
public class
    GetCallerWithPrivateInterHostTestingOnlyRequest : WebRequest<GetCallerWithPrivateInterHostTestingOnlyRequest,
    GetCallerTestingOnlyResponse>;
#endif