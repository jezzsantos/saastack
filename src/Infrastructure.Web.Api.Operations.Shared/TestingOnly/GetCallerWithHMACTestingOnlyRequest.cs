#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authn/hmac/get", ServiceOperation.Get, AccessType.HMAC, true)]
[UsedImplicitly]
public class GetCallerWithHMACTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>
{
}
#endif