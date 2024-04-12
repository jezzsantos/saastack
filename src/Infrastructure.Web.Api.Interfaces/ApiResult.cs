using Common;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a callback that returns an <see cref="EmptyResponse" /> or <see cref="Error" />.
///     Supported for all Methods: Post, Get, Search, PutPatch and Delete, wishing not to contain a response.
/// </summary>
public delegate Result<EmptyResponse, Error> ApiEmptyResult();

/// <summary>
///     Defines a callback that returns any <see cref="TResponse" /> or <see cref="Error" />.
///     Supported for most Methods: Get, Search, PutPatch and Delete.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiResult<TResource, TResponse>()
    where TResponse : IWebResponse;

/// <summary>
///     Defines a callback that returns a <see cref="PostResult{TResponse}" /> or <see cref="Error" />.
///     Supported for only Methods: Post
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<PostResult<TResponse>, Error> ApiPostResult<TResource, TResponse>()
    where TResponse : IWebResponse;

/// <summary>
///     Defines a callback that returns any <see cref="TResponse" /> or <see cref="Error" />.
///     Supported for only Methods: PutPatch
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiPutPatchResult<TResource, TResponse>()
    where TResponse : IWebResponse;

/// <summary>
///     Defines a callback that returns any <see cref="TResponse" /> or <see cref="Error" />.
///     Supported for only Methods: Get, Search.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiGetResult<TResource, TResponse>()
    where TResponse : IWebResponse;

/// <summary>
///     Defines a callback that returns any <see cref="Stream" /> or <see cref="Error" />.
///     Supported for only Methods: Get
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<StreamResult, Error> ApiStreamResult();

/// <summary>
///     Defines a callback that returns any <see cref="TResponse" /> or <see cref="Error" />.
///     Supported for only Methods: Search.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiSearchResult<TResource, TResponse>()
    where TResponse : IWebSearchResponse;

/// <summary>
///     Defines a callback that returns an <see cref="EmptyResponse" /> or <see cref="Error" />.
///     Supported for only Methods: Delete.
/// </summary>
public delegate Result<EmptyResponse, Error> ApiDeleteResult();

/// <summary>
///     Provides a container with a <see cref="TResponse" /> and other attributes describing a POST result
/// </summary>
public class PostResult<TResponse>
    where TResponse : IWebResponse
{
    public PostResult(TResponse response, string? resourceLocation = null)
    {
        Response = response;
        Location = resourceLocation;
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

/// <summary>
///     Provides a container describing a stream result
/// </summary>
public class StreamResult
{
    public StreamResult(Stream stream, string contentType)
    {
        Stream = stream;
        ContentType = contentType;
    }

    public string ContentType { get; }

    public Stream Stream { get; }
}