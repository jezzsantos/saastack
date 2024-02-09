using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/auth/logout", ServiceOperation.Post)]
public class LogoutRequest : UnTenantedEmptyRequest
{
}