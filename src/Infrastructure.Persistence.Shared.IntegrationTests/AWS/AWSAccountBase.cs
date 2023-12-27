using IntegrationTesting.Persistence.Common;

namespace Infrastructure.Persistence.Shared.IntegrationTests.AWS;

public static class AWSAccountBase
{
    public static void CleanupAllTests()
    {
        AWSLocalStackEmulator.Shutdown();
    }

    public static void InitializeAllTests()
    {
        AWSLocalStackEmulator.Start();
    }
}