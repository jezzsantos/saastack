using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class DependencyExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="scope" /> to a <see cref="ServiceLifetime" />
    /// </summary>
    public static ServiceLifetime ToLifetime(this DependencyScope scope)
    {
        return scope switch
        {
            DependencyScope.Platform => ServiceLifetime.Singleton,
            DependencyScope.PerTenant => ServiceLifetime.Scoped,
            DependencyScope.NotSharedSingleton => ServiceLifetime.Singleton,
            DependencyScope.NotSharedScoped => ServiceLifetime.Scoped,
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }
}