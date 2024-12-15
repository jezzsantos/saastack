namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running Kurrent for integration testing.
/// </summary>
public static class KurrentEmulator
{
    private static readonly string ContainerName = "kurrent";

    public static void Shutdown()
    {
        DockerImageEmulator.Shutdown();
    }

    public static void Start()
    {
        DockerImageEmulator.Start(ContainerName);
    }
}