#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with feature authorization
/// </summary>
[Route("/testingonly/authz/feature/get", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Features.Platform_PaidTrial)]
public class
    AuthorizeByFeatureTestingOnlyRequest : WebRequest<AuthorizeByFeatureTestingOnlyRequest,
    GetCallerTestingOnlyResponse>;
#endif