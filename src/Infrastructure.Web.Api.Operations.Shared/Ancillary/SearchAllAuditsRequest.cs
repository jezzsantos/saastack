using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Lists all audits since the specified date, for the specified organization
/// </summary>
[Route("/audits", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class SearchAllAuditsRequest : UnTenantedSearchRequest<SearchAllAuditsRequest, SearchAllAuditsResponse>
{
    public string? OrganizationId { get; set; }

    public DateTime? SinceUtc { get; set; }
}