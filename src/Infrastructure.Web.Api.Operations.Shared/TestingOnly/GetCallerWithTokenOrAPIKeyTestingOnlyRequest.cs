#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authn/token/get", ServiceOperation.Get, AccessType.Token, true)]
[UsedImplicitly]
public class GetCallerWithTokenOrAPIKeyTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>
{
}
#endif