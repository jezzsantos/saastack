using Common;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Defines an callback that relates a <see cref="PostResult{TResponse}" /> containing a
///     <see cref="TResource" />
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<PostResult<TResponse>, Error> ApiPostResult<TResource, TResponse>()
    where TResource : class where TResponse : IWebResponse;

/// <summary>
///     Defines an callback that relates a <see cref="TResponse" /> containing a
///     <see cref="TResource" />
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiResult<TResource, TResponse>()
    where TResource : class where TResponse : IWebResponse;

/// <summary>
///     Defines an callback that relates a <see cref="EmptyResponse" />
/// </summary>
public delegate Result<EmptyResponse, Error> ApiEmptyResult();

/// <summary>
///     Provides a container with a <see cref="TResponse" /> and other attributes describing a
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public class PostResult<TResponse>
    where TResponse : IWebResponse
{
    public PostResult(TResponse response, string? location = null)
    {
        Response = response;
        Location = location;
    }

    public string? Location { get; }

    public TResponse Response { get; }

    /// <summary>
    ///     Converts the <see cref="response" /> into a <see cref="PostResult{TResponse}" />
    /// </summary>
    public static implicit operator PostResult<TResponse>(TResponse response)
    {
        return new PostResult<TResponse>(response);
    }
}