#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Destroys all repositories
/// </summary>
[Route("/testingonly/repositories/destroy", OperationMethod.Post, isTestingOnly: true)]
public class DestroyAllRepositoriesRequest : UnTenantedEmptyRequest;
#endif