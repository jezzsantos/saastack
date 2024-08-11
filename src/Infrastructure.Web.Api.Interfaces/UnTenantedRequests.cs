namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API not for an Organization, with an empty response
/// </summary>
public abstract class UnTenantedEmptyRequest<TRequest> : WebRequestEmpty<TRequest>, IUnTenantedRequest
    where TRequest : IWebRequest;

/// <summary>
///     Defines the request of a POST/GET/PUT/PATCH API not for an Organization
/// </summary>
public abstract class UnTenantedRequest<TRequest, TResponse> : WebRequest<TRequest, TResponse>, IUnTenantedRequest
    where TResponse : IWebResponse
    where TRequest : IWebRequest;

/// <summary>
///     Defines the request of a SEARCH API not for an Organization
/// </summary>
public abstract class UnTenantedSearchRequest<TRequest, TResponse> : SearchRequest<TRequest, TResponse>,
    IUnTenantedRequest
    where TResponse : IWebSearchResponse
    where TRequest : IWebRequest;

/// <summary>
///     Defines the request of a DELETE API not for an Organization
/// </summary>
public abstract class UnTenantedDeleteRequest<TRequest> : WebRequestVoid<TRequest>, IUnTenantedRequest
    where TRequest : IWebRequest;

/// <summary>
///     Defines the request of a GET API for a stream not for an Organization
/// </summary>
public abstract class UnTenantedStreamRequest<TRequest> : WebRequestStream<TRequest>, IUnTenantedRequest
    where TRequest : IWebRequest;