namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request not for a specific Tenant
/// </summary>
public interface IUnTenantedRequest
{
}

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API not for an Organization, with an empty response
/// </summary>
public class UnTenantedEmptyRequest : IWebRequest<EmptyResponse>, IUnTenantedRequest
{
}

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API not for an Organization
/// </summary>
public class UnTenantedRequest<TResponse> : IWebRequest<TResponse>, IUnTenantedRequest
    where TResponse : IWebResponse
{
}

/// <summary>
///     Defines the request of a SEARCH API not for an Organization
/// </summary>
public class UnTenantedSearchRequest<TResponse> : SearchRequest<TResponse>, IUnTenantedRequest
    where TResponse : IWebSearchResponse
{
}

/// <summary>
///     Defines the request of a DELETE API not for an Organization
/// </summary>
public class UnTenantedDeleteRequest : IWebRequestVoid, IUnTenantedRequest
{
}