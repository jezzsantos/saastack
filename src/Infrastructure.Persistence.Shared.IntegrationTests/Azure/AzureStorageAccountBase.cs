using IntegrationTesting.Persistence.Common;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

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