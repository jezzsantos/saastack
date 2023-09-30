using Microsoft.Extensions.Hosting;

namespace Infrastructure.WebApi.Common;

public static class HostEnvironmentExtensions
{
    /// <summary>
    ///     Whether we are in either <see cref="Environments.Development" /> or CI
    /// </summary>
    public static bool IsTestingOnly(this IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        return hostEnvironment.IsEnvironment(Environments.Development) || hostEnvironment.IsEnvironment("CI");
    }
}