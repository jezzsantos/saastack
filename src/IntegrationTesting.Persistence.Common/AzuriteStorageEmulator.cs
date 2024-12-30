using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;

namespace IntegrationTesting.Persistence.Common;

/// <summary>
///     An emulator for running Azurite Storage Emulator for integration testing.
/// </summary>
public class AzuriteStorageEmulator
{
    private const string DockerImageName = "mcr.microsoft.com/azure-storage/azurite:latest";

    private readonly AzuriteContainer _azurite = new AzuriteBuilder()
        .WithImage(DockerImageName)
        .WithInMemoryPersistence()
        .Build();

    public string GetConnectionString()
    {
        if (!IsRunning())
        {
            throw new InvalidOperationException(
                "Azurite emulator must be started before getting the connection string.");
        }

        return _azurite.GetConnectionString();
    }

    private bool IsRunning()
    {
        return _azurite.State == TestcontainersStates.Running;
    }

    public async Task StartAsync()
    {
        await _azurite.StartAsync();
    }

    public async Task StopAsync()
    {
        await _azurite.DisposeAsync();
    }
}