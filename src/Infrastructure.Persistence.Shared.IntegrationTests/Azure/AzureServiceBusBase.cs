namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

/// <summary>
///     HACK: we need to connect to a real Azure Service Bus in Azure, since there is no emulator for it yet (coming soon)
///     We do this with a `ApplicationServices:Persistence:AzureServiceBus:ConnectionString` in the
///     `appsettings.Testing.local.json` file
/// </summary>
public static class AzureServiceBusBase
{
    public static void CleanupAllTests()
    {
    }

    public static void InitializeAllTests()
    {
    }
}