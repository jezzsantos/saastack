using Common.Recording;
using Infrastructure.Persistence.OnPremises.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.OnPremises;

[CollectionDefinition("SqlServerStore", DisableParallelization = true)]
public class AllSqlServerStoreSpecs : ICollectionFixture<SqlServerStorageSpecSetup>;

[UsedImplicitly]
public class SqlServerStorageSpecSetup : StoreSpecSetupBase, IAsyncLifetime
{
    private const string DockerImageName = "mcr.microsoft.com/mssql/server:2022-latest";
    private const int SuccessExitCode = 0;

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage(DockerImageName)
        .Build();

    private SqlServerStore _store = null!;

    public IDataStore DataStore => _store;

    public IEventStore EventStore => _store;

    public IBlobStore BlobStore => _store;
    
    private async Task RebuildDatabaseSchema(string databaseName)
    {
        await _sqlServer.ExecScriptAsync($"CREATE DATABASE {databaseName}");
        var schemaScriptFileName = "TestDatabaseSchema.sql";
        await _sqlServer.CopyAsync(Path.Combine(Environment.CurrentDirectory, "OnPremises", schemaScriptFileName), "/tmp/");

        var sqlCmd = await _sqlServer.GetSqlCmdFilePathAsync();
        var result = await _sqlServer.ExecAsync([
            sqlCmd,
            "-C",
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

        _store = SqlServerStore.Create(NoOpRecorder.Instance, SqlServerStoreOptions.CustomConnectionString(connectionString));
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
