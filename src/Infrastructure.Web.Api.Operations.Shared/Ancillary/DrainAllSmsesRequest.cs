#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Drains all the pending SMS messages
/// </summary>
[Route("/smses/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllSmsesRequest : UnTenantedEmptyRequest<DrainAllSmsesRequest>;
#endif