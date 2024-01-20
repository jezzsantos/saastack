using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/emails", ServiceOperation.Search, AccessType.Token)]
public class SearchEmailDeliveriesRequest : UnTenantedSearchRequest<SearchEmailDeliveriesResponse>
{
    public DateTime? SinceUtc { get; set; }
}