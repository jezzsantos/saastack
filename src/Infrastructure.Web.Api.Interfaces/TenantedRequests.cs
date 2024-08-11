using System.ComponentModel;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API for an Organization, with an empty response
/// </summary>
public abstract class TenantedEmptyRequest<TRequest> : WebRequestEmpty<TRequest>, ITenantedRequest
    where TRequest : IWebRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API for an Organization
/// </summary>
public abstract class TenantedRequest<TRequest, TResponse> : WebRequest<TRequest, TResponse>, ITenantedRequest
    where TResponse : IWebResponse
    where TRequest : IWebRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a SEARCH API for an Organization
/// </summary>
public abstract class TenantedSearchRequest<TRequest, TResponse> : SearchRequest<TRequest, TResponse>, ITenantedRequest
    where TResponse : IWebSearchResponse
    where TRequest : IWebRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a DELETE API for an Organization
/// </summary>
public abstract class TenantedDeleteRequest<TRequest> : WebRequestVoid<TRequest>, ITenantedRequest
    where TRequest : IWebRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines the request of a GET API for a stream for an Organization
/// </summary>
public abstract class TenantedStreamRequest<TRequest> : WebRequestStream<TRequest>, ITenantedRequest
    where TRequest : IWebRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}