#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/security/none", OperationMethod.Post, AccessType.Anonymous, true)]
public class PostInsecureTestingOnlyRequest : UnTenantedEmptyRequest;
#endif