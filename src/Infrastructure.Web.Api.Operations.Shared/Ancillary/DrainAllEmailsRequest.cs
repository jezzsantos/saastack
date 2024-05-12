#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Drains all the pending email messages
/// </summary>
[Route("/emails/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllEmailsRequest : UnTenantedEmptyRequest;
#endif