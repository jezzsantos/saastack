using IntegrationTesting.Persistence.Common;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

public static class KurrentBase
{
    public static void CleanupAllTests()
    {
        KurrentEmulator.Shutdown();
    }

    public static void InitializeAllTests()
    {
        KurrentEmulator.Start();
    }
}