#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authn/token/get", ServiceOperation.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class GetCallerWithTokenOrAPIKeyTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>
{
}
#endif