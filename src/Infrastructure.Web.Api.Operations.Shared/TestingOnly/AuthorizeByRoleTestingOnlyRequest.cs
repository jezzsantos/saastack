#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/authz/role/get", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class AuthorizeByRoleTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>;
#endif