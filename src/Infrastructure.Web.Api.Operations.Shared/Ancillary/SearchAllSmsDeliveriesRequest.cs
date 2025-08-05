using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all SMS deliveries since the specified date, for the specified timeframe, organization and tags
/// </summary>
[Route("/smses", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class
    SearchAllSmsDeliveriesRequest : UnTenantedSearchRequest<SearchAllSmsDeliveriesRequest,
    SearchAllSmsDeliveriesResponse>
{
    public string? OrganizationId { get; set; }

    public DateTime? SinceUtc { get; set; }

    public string[]? Tags { get; set; }
}