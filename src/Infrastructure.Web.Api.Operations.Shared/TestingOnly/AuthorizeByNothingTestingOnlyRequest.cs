#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with no authorization
/// </summary>
[Route("/testingonly/authz/none/get", OperationMethod.Get, AccessType.Anonymous, true)]
public class AuthorizeByNothingTestingOnlyRequest : IWebRequest<GetCallerTestingOnlyResponse>;
#endif