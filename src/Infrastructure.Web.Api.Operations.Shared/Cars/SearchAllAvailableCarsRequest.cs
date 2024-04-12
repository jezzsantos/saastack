using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/available", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class SearchAllAvailableCarsRequest : TenantedSearchRequest<SearchAllCarsResponse>
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}