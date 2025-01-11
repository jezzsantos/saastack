using Common.Recording;
using Infrastructure.Persistence.Azure.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[CollectionDefinition("AzureSqlServerStore", DisableParallelization = true)]
public class AllAzureSqlServerStoreSpecs : ICollectionFixture<AzureSqlServerStoreSpecSetup>;

[UsedImplicitly]
public class AzureSqlServerStoreSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private const string DockerImageName = "mcr.microsoft.com/mssql/server:2022-latest";
    private const int SuccessExitCode = 0;

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage(DockerImageName)
        .Build();

    private AzureSqlServerStore _store = null!;

    public IDataStore DataStore => _store;

    public IEventStore EventStore => _store;

    private async Task RebuildDatabaseSchema(string databaseName)
    {
        await _sqlServer.ExecScriptAsync($"CREATE DATABASE {databaseName}");
        var schemaScriptFileName = "TestDatabaseSchema.sql";
        await _sqlServer.CopyAsync(Path.Combine(Environment.CurrentDirectory, "Azure", schemaScriptFileName), "/tmp/");

        var sqlCmd = await _sqlServer.GetSqlCmdFilePathAsync(); // need full path to sqlcmd as it's not found in $PATH
        var result = await _sqlServer.ExecAsync([
            sqlCmd,
            "-C", // trust the server certificate without validation
            "-d", databaseName,
            "-i", $"/tmp/{schemaScriptFileName}"
        ]);

        if (result.ExitCode != SuccessExitCode)
        {
            throw new Exception($"Failed to execute {schemaScriptFileName}: {result.Stderr}");
        }
    }

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        var databaseName = Settings.GetString("ApplicationServices:Persistence:SqlServer:DbName");
        await RebuildDatabaseSchema(databaseName);

        var connectionString = GetConnectionStringForDatabase(databaseName);

        _store = AzureSqlServerStore.Create(NoOpRecorder.Instance, AzureSqlServerStoreOptions.CustomConnectionString(connectionString));
    }

    private string GetConnectionStringForDatabase(string databaseName)
    {
        return new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ToString();
    }

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
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