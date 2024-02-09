#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/security/none", ServiceOperation.Post, AccessType.Anonymous, true)]
public class PostInsecureTestingOnlyRequest : UnTenantedEmptyRequest
{
}
#endif