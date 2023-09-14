using System.Diagnostics.CodeAnalysis;
using Infrastructure.WebApi.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Common;

public static class WebExtensions
{
    public static WebApplication MediateGet<TRequest, TResponse>(this WebApplication app,
        [StringSyntax("Route")] string routeTemplate)
        where TRequest : IWebRequest<TResponse> where TResponse : IWebResponse
    {
        app.MapGet(routeTemplate,
            async (IMediator mediator, [AsParameters] TRequest request) => await mediator.Send(request));

        return app;
    }
}