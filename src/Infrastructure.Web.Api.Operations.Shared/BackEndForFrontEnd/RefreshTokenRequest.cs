using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Refreshes the current user's authentication session (if possible)
/// </summary>
[Route("/auth/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedEmptyRequest<RefreshTokenRequest>;