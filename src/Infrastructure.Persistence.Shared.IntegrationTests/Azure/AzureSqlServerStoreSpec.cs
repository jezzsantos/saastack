using System.Diagnostics;
using System.ServiceProcess;
using Common.Extensions;
using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[CollectionDefinition("AzureSqlServerStore", DisableParallelization = true)]
public class AllAzureSqlServerStoreSpecs : ICollectionFixture<AzureSqlServerStoreSpecSetup>;

[UsedImplicitly]
public class AzureSqlServerStoreSpecSetup : StoreSpecSetupBase, IDisposable
{
    private const string CreateDatabaseCliCommandArgs = "-Q \"CREATE DATABASE {0}\"";
    private const string RegenerateCliCommandArgs = "-i \"{0}\\Azure\\TestDatabaseSchema.sql\"";
    private const string SqlServiceCli = @"SQLCMD";
    private readonly string _serviceName;
    private readonly AzureSqlServerStore _store;

    public AzureSqlServerStoreSpecSetup()
    {
        _serviceName = Settings.GetString("ApplicationServices:Persistence:SqlServer:LocalServiceName");
        var databaseName = Settings.GetString("ApplicationServices:Persistence:SqlServer:DbName");
        EnsureLocalDatabaseServiceIsStarted(Environment.CurrentDirectory, _serviceName, databaseName);

        _store = AzureSqlServerStore.Create(NoOpRecorder.Instance, Settings);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ShutdownLocalDatabaseService(_serviceName);
        }
    }

    public IDataStore DataStore => _store;

    public IEventStore EventStore => _store;

    private static void EnsureLocalDatabaseServiceIsStarted(string deploymentDirectory, string serviceName,
        string databaseName)
    {
        if (!IsLocalDatabaseServiceRunning(serviceName))
        {
            StartLocalDatabaseService(serviceName);
            while (!IsLocalDatabaseServiceRunning(serviceName))
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        RebuildDatabaseSchema(databaseName, deploymentDirectory);
    }

    private static bool IsLocalDatabaseServiceRunning(string serviceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Cannot run tests on SQLServer on a non-Windows platform");
        }

        using var controller = new ServiceController(serviceName);
        return controller.Status == ServiceControllerStatus.Running;
    }

    private static void StartLocalDatabaseService(string serviceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Cannot run tests on SQLServer on a non-Windows platform");
        }

        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Stopped)
        {
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running);
        }
    }

    private static void RebuildDatabaseSchema(string databaseName, string scriptPath)
    {
        ExecuteSqlCommand(SqlServiceCli, CreateDatabaseCliCommandArgs.Format(databaseName));
        ExecuteSqlCommand(SqlServiceCli, RegenerateCliCommandArgs.Format(scriptPath));
    }

    private static void ExecuteSqlCommand(string command, string arguments, bool waitForCompletion = true)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            Arguments = arguments,
            FileName = command,
            WindowStyle = ProcessWindowStyle.Hidden,
            Verb = "runas",
            UseShellExecute = false,
            RedirectStandardError = true
        });
        if (waitForCompletion)
        {
            process!.WaitForExit();
        }

        if (process!.ExitCode != 0)
        {
            throw new Exception(process.StandardError.ReadToEnd());
        }
    }

    private static void ShutdownLocalDatabaseService(string serviceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Cannot run tests on SQLServer on a non-Windows platform");
        }

        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Running)
        {
            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("AzureSqlServerStore")]
[UsedImplicitly]
public class AzureSqlServerDataStoreSpec : AnyDataStoreBaseSpec
{
    public AzureSqlServerDataStoreSpec(AzureSqlServerStoreSpecSetup setup) : base(setup.DataStore)
    {
    }
}

[Trait("Category", "Integration.Persistence")]
[Collection("AzureSqlServerStore")]
[UsedImplicitly]
public class AzureSqlServerEventStoreSpec : AnyEventStoreBaseSpec
{
    public AzureSqlServerEventStoreSpec(AzureSqlServerStoreSpecSetup setup) : base(setup.EventStore)
    {
    }
}