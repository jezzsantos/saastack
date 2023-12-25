using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Hosting.Common.Extensions;

public static class DependencyExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="scope" /> to a
    ///     <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime" />
    /// </summary>
    public static ServiceLifetime ToLifetime(this DependencyScope scope)
    {
        return scope switch
        {
            DependencyScope.UnTenanted => ServiceLifetime.Singleton,
            DependencyScope.Tenanted => ServiceLifetime.Scoped,
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }
}