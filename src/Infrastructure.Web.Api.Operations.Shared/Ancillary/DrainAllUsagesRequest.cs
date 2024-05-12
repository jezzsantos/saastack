#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Drains all the pending usage messages
/// </summary>
[Route("/usages/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllUsagesRequest : UnTenantedEmptyRequest;
#endif