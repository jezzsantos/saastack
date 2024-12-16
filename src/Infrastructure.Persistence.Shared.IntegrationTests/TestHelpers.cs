using Common.Extensions;
using Infrastructure.Hosting.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public static class TestHelpers
{
    /// <summary>
    ///     Create settings for testing purposes
    /// </summary>
    /// <param name="overrides">Any additional settings to override the appsettings configuration files</param>
    /// <returns>The resulting settings from combining the configuration files and any overrides</returns>
    public static AspNetDynamicConfigurationSettings CreateTestSettings(Dictionary<string, string?>? overrides = null)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json", true)
            .AddJsonFile("appsettings.Testing.local.json", true);

        if (overrides.Exists())
        {
            configurationBuilder.AddInMemoryCollection(overrides);
        }

        var configuration = configurationBuilder.Build();
        return new AspNetDynamicConfigurationSettings(configuration);
    }
}