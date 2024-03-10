using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/auth/refresh", ServiceOperation.Post)]
public class RefreshTokenRequest : UnTenantedEmptyRequest;