#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authz/feature/get", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Features.Platform_PaidTrial)]
public class AuthorizeByFeatureTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>;
#endif