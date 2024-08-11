namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a request that returns an empty response
/// </summary>
public abstract class WebRequestEmpty<TRequest> : WebRequest<TRequest, EmptyResponse>
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="TResponse" /> response
/// </summary>
public abstract class WebRequest<TRequest, TResponse> : WebRequest<TRequest>, IWebRequest<TResponse>
    where TResponse : IWebResponse
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="TResponse" /> response
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
// ReSharper disable once UnusedTypeParameter
public abstract partial class WebRequest<TRequest> : IWebRequest
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns no response
/// </summary>
public abstract class WebRequestVoid<TRequest> : WebRequest<TRequest>, IWebRequestVoid
    where TRequest : IWebRequest
{
}

/// <summary>
///     Defines an incoming REST request that returns a <see cref="Stream" />
/// </summary>
public abstract class WebRequestStream<TRequest> : WebRequest<TRequest>, IWebRequestStream
    where TRequest : IWebRequest
{
}