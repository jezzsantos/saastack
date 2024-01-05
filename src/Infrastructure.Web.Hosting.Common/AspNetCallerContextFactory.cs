using Application.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a factory for retrieving new instances of the <see cref="ICallerContext" />
/// </summary>
public sealed class AspNetCallerContextFactory : ICallerContextFactory
{
    private readonly IDependencyContainer _container;

    public AspNetCallerContextFactory(IDependencyContainer container)
    {
        _container = container;
    }

    public ICallerContext Create()
    {
        return new AspNetCallerContext(_container.Resolve<IHttpContextAccessor>());
    }
}