#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authn/hmac/get", OperationMethod.Get, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class GetCallerWithHMACTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>;
#endif