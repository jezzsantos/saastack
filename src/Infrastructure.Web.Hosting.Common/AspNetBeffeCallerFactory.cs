using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Interfaces.Services;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a factory for retrieving new instances of the <see cref="ICallerContext" />,
///     for BEFFE hosts that have no Authentication and Authorization of their own
/// </summary>
public sealed class AspNetBeffeCallerFactory : ICallerContextFactory
{
    private readonly IDependencyContainer _container;

    public AspNetBeffeCallerFactory(IDependencyContainer container)
    {
        _container = container;
    }

    public ICallerContext Create()
    {
        return new AspNetBeffeCallerContext(_container.GetRequiredService<IHostSettings>(),
            _container.GetRequiredService<IHttpContextAccessor>());
    }
}