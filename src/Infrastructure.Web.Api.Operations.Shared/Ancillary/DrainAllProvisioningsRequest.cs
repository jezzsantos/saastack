#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/provisioning/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllProvisioningsRequest : UnTenantedEmptyRequest;
#endif