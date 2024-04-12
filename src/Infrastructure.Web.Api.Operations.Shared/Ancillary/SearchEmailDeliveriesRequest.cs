using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/emails", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class SearchEmailDeliveriesRequest : UnTenantedSearchRequest<SearchEmailDeliveriesResponse>
{
    public DateTime? SinceUtc { get; set; }
}