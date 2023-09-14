using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Interfaces;

public interface IWebRequest<TResponse> : IRequest<IResult> where TResponse : IWebResponse
{
}