#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with HMAC signature authentication
/// </summary>
[Route("/testingonly/authn/hmac/get", OperationMethod.Get, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class
    GetCallerWithHMACTestingOnlyRequest : WebRequest<GetCallerWithHMACTestingOnlyRequest, GetCallerTestingOnlyResponse>;
#endif