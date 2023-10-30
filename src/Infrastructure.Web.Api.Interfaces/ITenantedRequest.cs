namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request for an Organization
/// </summary>
public interface ITenantedRequest
{
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API for an Organization
/// </summary>
public class TenantedRequest<TResponse> : IWebRequest<TResponse>, ITenantedRequest
    where TResponse : IWebResponse
{
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a SEARCH API for an Organization
/// </summary>
public class TenantedSearchRequest<TResponse> : SearchRequest<TResponse>, ITenantedRequest
    where TResponse : IWebResponse
{
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a DELETE API for an Organization
/// </summary>
public class TenantedDeleteRequest : IWebRequestVoid, ITenantedRequest
{
    public string? OrganizationId { get; set; }
}