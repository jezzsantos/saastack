using Common.Configuration;
using Infrastructure.Hosting.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public abstract class StoreSpecSetupBase
{
    protected StoreSpecSetupBase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(@"appsettings.Testing.json")
            .Build();
        Settings = new AspNetConfigurationSettings(configuration).Platform;
    }

    protected ISettings Settings { get; }
}