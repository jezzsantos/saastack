using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all email deliveries since the specified date, for the specified timeframe, organization and tags
/// </summary>
[Interfaces.Route("/emails", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class
    SearchAllEmailDeliveriesRequest : UnTenantedSearchRequest<SearchAllEmailDeliveriesRequest,
    SearchAllEmailDeliveriesResponse>
{
    public string? OrganizationId { get; set; }

    public DateTime? SinceUtc { get; set; }

    [FromQuery] public string[]? Tags { get; set; }
}