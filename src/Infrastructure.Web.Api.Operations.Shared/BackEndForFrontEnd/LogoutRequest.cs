using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Removes the current user's authenticated session (if any)
/// </summary>
[Route("/auth/logout", OperationMethod.Post)]
public class LogoutRequest : UnTenantedEmptyRequest;