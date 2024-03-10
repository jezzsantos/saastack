namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a incoming REST request and response.
///     Note: we have split this interface definition so it can be reused in Roslyn components
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public partial interface IWebRequest;

/// <summary>
///     Defines a incoming REST request and response.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public interface IWebRequest<TResponse> : IWebRequest
    where TResponse : IWebResponse;

/// <summary>
///     Defines a incoming REST request and response.
/// </summary>
public interface IWebRequestVoid : IWebRequest;