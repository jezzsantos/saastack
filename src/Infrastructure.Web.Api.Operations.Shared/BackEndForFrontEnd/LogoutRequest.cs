using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/auth/logout", OperationMethod.Post)]
public class LogoutRequest : UnTenantedEmptyRequest;