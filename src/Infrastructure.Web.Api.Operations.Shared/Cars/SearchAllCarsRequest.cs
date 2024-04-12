using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class SearchAllCarsRequest : TenantedSearchRequest<SearchAllCarsResponse>;