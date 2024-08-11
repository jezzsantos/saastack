using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all email deliveries since the specified date
/// </summary>
[Route("/emails", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class
    SearchEmailDeliveriesRequest : UnTenantedSearchRequest<SearchEmailDeliveriesRequest, SearchEmailDeliveriesResponse>
{
    public DateTime? SinceUtc { get; set; }
}