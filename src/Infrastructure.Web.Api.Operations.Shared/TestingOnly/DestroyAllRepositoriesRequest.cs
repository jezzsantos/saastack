#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/repositories/destroy", ServiceOperation.Post, isTestingOnly: true)]
public class DestroyAllRepositoriesRequest : UnTenantedEmptyRequest;
#endif