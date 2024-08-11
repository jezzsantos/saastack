#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with role authorization
/// </summary>
[Route("/testingonly/authz/role/get", OperationMethod.Get, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class
    AuthorizeByRoleTestingOnlyRequest : WebRequest<AuthorizeByRoleTestingOnlyRequest, GetCallerTestingOnlyResponse>;
#endif