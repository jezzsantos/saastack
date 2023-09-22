using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a incoming REST request payload
/// </summary>
// ReSharper disable once UnusedTypeParameter
public interface IWebRequest<TResponse> : IRequest<IResult> where TResponse : IWebResponse
{
}