namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines an incoming REST request.
///     Note: we have split this interface definition so that it can be reused in Roslyn components
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public partial interface IWebRequest;

/// <summary>
///     Defines an incoming REST request with response.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public interface IWebRequest<TResponse> : IWebRequest
    where TResponse : IWebResponse;

/// <summary>
///     Defines an incoming REST request with empty response.
/// </summary>
public interface IWebRequestVoid : IWebRequest;

/// <summary>
///     Defines an incoming REST request with a stream response.
/// </summary>
public interface IWebRequestStream : IWebRequest;