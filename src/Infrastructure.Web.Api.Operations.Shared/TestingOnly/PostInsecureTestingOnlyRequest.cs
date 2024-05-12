#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with anonymous access
/// </summary>
[Route("/testingonly/security/none", OperationMethod.Post, AccessType.Anonymous, true)]
public class PostInsecureTestingOnlyRequest : UnTenantedEmptyRequest;
#endif