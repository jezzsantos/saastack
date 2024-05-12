using System.ComponentModel;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API for an Organization, with an empty response
/// </summary>
public class TenantedEmptyRequest : IWebRequest<EmptyResponse>, ITenantedRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API for an Organization
/// </summary>
public class TenantedRequest<TResponse> : IWebRequest<TResponse>, ITenantedRequest
    where TResponse : IWebResponse
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a SEARCH API for an Organization
/// </summary>
public class TenantedSearchRequest<TResponse> : SearchRequest<TResponse>, ITenantedRequest
    where TResponse : IWebSearchResponse
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a DELETE API for an Organization
/// </summary>
public class TenantedDeleteRequest : IWebRequestVoid, ITenantedRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a GET API for a stream for an Organization
/// </summary>
public class TenantedStreamRequest : IWebRequestStream, ITenantedRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}