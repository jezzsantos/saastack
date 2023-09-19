using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Interfaces;

// ReSharper disable once UnusedTypeParameter
public interface IWebRequest<TResponse> : IRequest<IResult> where TResponse : IWebResponse
{
}