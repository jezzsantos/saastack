// ReSharper disable once CheckNamespace

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a incoming REST request and response.
/// </summary>
public interface IWebRequest
{
}

/// <summary>
///     Defines a incoming REST request and response.
/// </summary>
// ReSharper disable once UnusedTypeParameter
public interface IWebRequest<TResponse> : IWebRequest
    where TResponse : IWebResponse
{
}