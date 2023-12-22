using IntegrationTesting.Persistence.Common;

namespace Infrastructure.Api.WorkerHost.IntegrationTests.AzureFunctions;

public static class AzureStorageAccountBase
{
    public static void CleanupAllTests()
    {
        AzuriteStorageEmulator.Shutdown();
    }

    public static void InitializeAllTests()
    {
        AzuriteStorageEmulator.Start();
    }
}