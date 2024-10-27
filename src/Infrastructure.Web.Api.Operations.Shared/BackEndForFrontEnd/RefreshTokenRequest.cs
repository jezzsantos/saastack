using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Refreshes the current user's authentication session (if possible)
/// </summary>
/// <response code="401">The refresh token has expired</response>
/// <response code="423">The user's account is suspended or disabled, and cannot be authenticated or used</response>
[Route("/auth/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedEmptyRequest<RefreshTokenRequest>;