using Common.Configuration;
using Infrastructure.Hosting.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public abstract class StoreSpecSetupBase
{
    protected StoreSpecSetupBase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json", true)
            .AddJsonFile("appsettings.Testing.local.json", true)
            .Build();
        Settings = new AspNetDynamicConfigurationSettings(configuration);
    }

    protected IConfigurationSettings Settings { get; }
}