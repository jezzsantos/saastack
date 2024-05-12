#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with token authentication
/// </summary>
[Route("/testingonly/authn/token/get", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class GetCallerWithTokenOrAPIKeyTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>;
#endif