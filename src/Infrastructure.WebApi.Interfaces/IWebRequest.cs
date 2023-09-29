using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a incoming REST request and response.
///     Note: <see cref="IRequest{IResult}" /> is required for the MediatR handlers to be wired up
/// </summary>
public interface IWebRequest : IRequest<IResult>
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

/// <summary>
///     Defines a incoming REST request and response.
/// </summary>
public interface IWebRequestVoid : IWebRequest
{
}