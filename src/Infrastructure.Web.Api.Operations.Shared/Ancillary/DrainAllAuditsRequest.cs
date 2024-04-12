#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/audits/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllAuditsRequest : UnTenantedEmptyRequest;
#endif