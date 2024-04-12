#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/security/none", OperationMethod.Get, AccessType.Anonymous, true)]
public class GetInsecureTestingOnlyRequest : UnTenantedEmptyRequest;
#endif