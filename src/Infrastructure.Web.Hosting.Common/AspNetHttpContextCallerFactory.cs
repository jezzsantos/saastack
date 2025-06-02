using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Interfaces.Services;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a factory for retrieving new instances of the <see cref="ICallerContext" />,
///     for API hosts that have already configured Authentication and Authorization
/// </summary>
public sealed class AspNetHttpContextCallerFactory : ICallerContextFactory
{
    private readonly IDependencyContainer _container;

    public AspNetHttpContextCallerFactory(IDependencyContainer container)
    {
        _container = container;
    }

    public ICallerContext Create()
    {
        return new AspNetClaimsBasedCallerContext(_container.GetRequiredService<ITenancyContext>(),
            _container.GetRequiredService<IHostSettings>(), _container.GetRequiredService<IHttpContextAccessor>());
    }
}