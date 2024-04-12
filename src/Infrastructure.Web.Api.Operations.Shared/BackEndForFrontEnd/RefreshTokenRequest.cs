using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/auth/refresh", OperationMethod.Post)]
public class RefreshTokenRequest : UnTenantedEmptyRequest;